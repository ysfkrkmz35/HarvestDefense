using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.VFX;
using UnityEngine.Profiling;


namespace HappyHarvest
{
    /// <summary>
    /// Manage everything related to the terrain where crop are planted. Hold the content of cells with the states of
    /// crop in those cells. Handle also switching tiles and the like where tilling and watering happens.
    /// </summary>
    public class TerrainManager : MonoBehaviour
    {
        [System.Serializable]
        public class GroundData
        {
            public const float WaterDuration = 60 * 1.0f;

            public float WaterTimer;
        }

        public class CropData
        {
            [Serializable]
            public struct SaveData
            {
                public string CropId;
                public int Stage;
                public float GrowthRatio;
                public float GrowthTimer;
                public int HarvestCount;
                public float DyingTimer;
            }
            
            public Crop GrowingCrop = null;
            public int CurrentGrowthStage = 0;

            public float GrowthRatio = 0.0f;
            public float GrowthTimer = 0.0f;

            public int HarvestCount = 0;
            
            public float DyingTimer;
            public bool HarvestDone => HarvestCount == GrowingCrop.NumberOfHarvest;

            public void Init()
            {
                GrowingCrop = null;
                GrowthRatio = 0.0f;
                GrowthTimer = 0.0f;
                CurrentGrowthStage = 0;
                HarvestCount = 0;

                DyingTimer = 0.0f;
            }

            public Crop Harvest()
            {
                var crop = GrowingCrop;

                HarvestCount += 1;

                CurrentGrowthStage = GrowingCrop.StageAfterHarvest;
                GrowthRatio = CurrentGrowthStage / (float)GrowingCrop.GrowthStagesTiles.Length;
                GrowthTimer = GrowingCrop.GrowthTime * GrowthRatio;

                return crop;
            }

            public void Save(ref SaveData data)
            {
                data.Stage = CurrentGrowthStage;
                data.CropId = GrowingCrop.Key;
                data.DyingTimer = DyingTimer;
                data.GrowthRatio = GrowthRatio;
                data.GrowthTimer = GrowthTimer;
                data.HarvestCount = HarvestCount;
            }

            public void Load(SaveData data)
            {
                CurrentGrowthStage = data.Stage;
                GrowingCrop = GameManager.Instance.CropDatabase.GetFromID(data.CropId);
                DyingTimer = data.DyingTimer;
                GrowthRatio = data.GrowthRatio;
                GrowthTimer = data.GrowthTimer;
                HarvestCount = data.HarvestCount;
            }
        }

        public Grid Grid;
        
        public Tilemap GroundTilemap;
        public Tilemap CropTilemap;
        
        [Header("Watering")]
        public Tilemap WaterTilemap;
        public TileBase WateredTile;
        
        [Header("Tilling")] 
        public TileBase TilleableTile;
        public TileBase TilledTile;
        public VisualEffect TillingEffectPrefab;
        
        private Dictionary<Vector3Int, GroundData> m_GroundData = new();
        private Dictionary<Vector3Int, CropData> m_CropData = new();

        private Dictionary<Crop, List<VisualEffect>> m_HarvestEffectPool = new();
        private List<VisualEffect> m_TillingEffectPool = new();
        
        // Optimization: Cache list of active watered cells and growing crops to avoid full dictionary iteration
        private List<Vector3Int> m_WateredCells = new();
        private List<Vector3Int> m_GrowingCrops = new();

        public bool IsTillable(Vector3Int target)
        {
            return GroundTilemap.GetTile(target) == TilleableTile;
        }

        public bool IsPlantable(Vector3Int target)
        {
            return IsTilled(target) && !m_CropData.ContainsKey(target);
        }

        public bool IsTilled(Vector3Int target)
        {
            return m_GroundData.ContainsKey(target);
        }

        public void TillAt(Vector3Int target)
        {
            if (IsTilled(target))
                return;
            
            GroundTilemap.SetTile(target, TilledTile);
            m_GroundData.Add(target, new GroundData());

            // Optimization: Cycle through pool without removing/adding (expensive operations)
            var inst = m_TillingEffectPool[0];
            m_TillingEffectPool.RemoveAt(0);
            m_TillingEffectPool.Add(inst);

            inst.gameObject.transform.position = Grid.GetCellCenterWorld(target);
            
            inst.Stop();
            inst.Play();
        }

        public void PlantAt(Vector3Int target, Crop cropToPlant)
        {
            var cropData = new CropData();
            
            cropData.GrowingCrop = cropToPlant;
            cropData.GrowthTimer = 0.0f;
            cropData.CurrentGrowthStage = 0;
            
            m_CropData.Add(target, cropData);
            m_GrowingCrops.Add(target); // Track for optimized updates
            
            UpdateCropVisual(target);

            if (!m_HarvestEffectPool.ContainsKey(cropToPlant))
            {
                InitHarvestEffect(cropToPlant);
            }
        }

        public void InitHarvestEffect(Crop crop)
        {
            m_HarvestEffectPool[crop] = new List<VisualEffect>();
            for (int i = 0; i < 4; ++i)
            {
                var inst = Instantiate(crop.HarvestEffect);
                inst.Stop();
                m_HarvestEffectPool[crop].Add(inst);
            }
        }

        public void WaterAt(Vector3Int target)
        {
            var groundData = m_GroundData[target];

            groundData.WaterTimer = GroundData.WaterDuration;
            
            // Track watered cells for optimized updates
            if (!m_WateredCells.Contains(target))
            {
                m_WateredCells.Add(target);
            }
            
            WaterTilemap.SetTile(target, WateredTile);
            //GroundTilemap.SetColor(target, WateredTiledColorTint);
        }

        public Crop HarvestAt(Vector3Int target)
        {
            m_CropData.TryGetValue(target, out var data);

            if (data == null || !Mathf.Approximately(data.GrowthRatio,1.0f)) return null;
            
            var produce = data.Harvest();

            if (data.HarvestDone)
            {
                m_CropData.Remove(target);
                m_GrowingCrops.Remove(target); // Remove from tracked list
            }
            
            UpdateCropVisual(target);

            var effect = m_HarvestEffectPool[data.GrowingCrop][0];
            effect.transform.position = Grid.GetCellCenterWorld(target);
            m_HarvestEffectPool[data.GrowingCrop].RemoveAt(0);
            m_HarvestEffectPool[data.GrowingCrop].Add(effect);
            effect.Play();

            return produce;
        }

        public CropData GetCropDataAt(Vector3Int target)
        {
            m_CropData.TryGetValue(target, out var data);
            return data;
        }

        public void OverrideGrowthStage(Vector3Int target, int newGrowthStage)
        {
            var data = GetCropDataAt(target);

            data.GrowthRatio = Mathf.Clamp01((newGrowthStage+1) / (float)data.GrowingCrop.GrowthStagesTiles.Length);
            data.GrowthTimer = data.GrowthRatio * data.GrowingCrop.GrowthTime;
            data.CurrentGrowthStage = newGrowthStage;
            
            UpdateCropVisual(target);
        }

        private void Awake()
        {
            GameManager.Instance.Terrain = this;

            for (int i = 0; i < 4; ++i)
            {
                var effect = Instantiate(TillingEffectPrefab);
                effect.gameObject.SetActive(true);
                effect.Stop();
                m_TillingEffectPool.Add(effect);
            }
        }

        private void Update()
        {
            Profiler.BeginSample("TerrainManager.UpdateWateredCells");
            // Optimization: Only iterate through watered cells instead of all ground data
            for (int i = m_WateredCells.Count - 1; i >= 0; i--)
            {
                var cell = m_WateredCells[i];
                if (!m_GroundData.TryGetValue(cell, out var groundData))
                {
                    m_WateredCells.RemoveAt(i);
                    continue;
                }
                
                if (groundData.WaterTimer > 0.0f)
                {
                    groundData.WaterTimer -= Time.deltaTime;

                    if (groundData.WaterTimer <= 0.0f)
                    {
                        WaterTilemap.SetTile(cell, null);
                        m_WateredCells.RemoveAt(i); // No longer watered
                        //GroundTilemap.SetColor(cell, Color.white);
                    }
                }
            }
            Profiler.EndSample();

            Profiler.BeginSample("TerrainManager.UpdateGrowingCrops");
            // Optimization: Only iterate through growing crops instead of all ground data
            for (int i = m_GrowingCrops.Count - 1; i >= 0; i--)
            {
                var cell = m_GrowingCrops[i];
                if (!m_CropData.TryGetValue(cell, out var cropData))
                {
                    m_GrowingCrops.RemoveAt(i);
                    continue;
                }
                
                if (!m_GroundData.TryGetValue(cell, out var groundData))
                {
                    continue;
                }

                if (groundData.WaterTimer <= 0.0f)
                {
                    cropData.DyingTimer += Time.deltaTime;
                    if (cropData.DyingTimer > cropData.GrowingCrop.DryDeathTimer)
                    {
                        m_CropData.Remove(cell);
                        m_GrowingCrops.RemoveAt(i);
                        UpdateCropVisual(cell);
                    }
                }
                else
                {
                    cropData.DyingTimer = 0.0f;
                    cropData.GrowthTimer = Mathf.Clamp(cropData.GrowthTimer + Time.deltaTime, 0.0f,
                        cropData.GrowingCrop.GrowthTime);
                    cropData.GrowthRatio = cropData.GrowthTimer / cropData.GrowingCrop.GrowthTime;
                    int growthStage = cropData.GrowingCrop.GetGrowthStage(cropData.GrowthRatio);

                    if (growthStage != cropData.CurrentGrowthStage)
                    {
                        cropData.CurrentGrowthStage = growthStage;
                        UpdateCropVisual(cell);
                    }
                }
            }
            Profiler.EndSample();
        }

        void UpdateCropVisual(Vector3Int target)
        {
            if (!m_CropData.TryGetValue(target, out var data))
            {
                CropTilemap.SetTile(target, null);
            }
            else
            {
                CropTilemap.SetTile(target, data.GrowingCrop.GrowthStagesTiles[data.CurrentGrowthStage]);
            }
        }

        public void Save(ref TerrainDataSave data)
        {
            data.GroundDatas = new List<GroundData>();
            data.GroundDataPositions = new List<Vector3Int>();

            foreach (var groundData in m_GroundData)
            {
                data.GroundDataPositions.Add(groundData.Key);
                data.GroundDatas.Add(groundData.Value);
            }

            data.CropDatas = new List<CropData.SaveData>();
            data.CropDataPositions = new List<Vector3Int>();

            foreach (var cropData in m_CropData)
            {
                data.CropDataPositions.Add(cropData.Key);

                var saveData = new CropData.SaveData();
                cropData.Value.Save(ref saveData);
                data.CropDatas.Add(saveData);
            }
        }

        public void Load(TerrainDataSave data)
        {
            m_GroundData = new Dictionary<Vector3Int, GroundData>();
            m_WateredCells.Clear(); // Clear tracked lists
            
            for (int i = 0; i < data.GroundDatas.Count; ++i)
            {
                var pos = data.GroundDataPositions[i];
                m_GroundData.Add(pos, data.GroundDatas[i]);
                
                GroundTilemap.SetTile(pos, TilledTile);
                
                bool isWatered = data.GroundDatas[i].WaterTimer > 0.0f;
                WaterTilemap.SetTile(data.GroundDataPositions[i], isWatered ? WateredTile : null);
                
                if (isWatered)
                {
                    m_WateredCells.Add(pos); // Track watered cells
                }
                //GroundTilemap.SetColor(data.GroundDataPositions[i], data.GroundDatas[i].WaterTimer > 0.0f ? WateredTiledColorTint : Color.white);
            }

            //clear all existing effect as we will reload new one
            foreach (var pool in m_HarvestEffectPool)
            {
                if (pool.Value != null)
                {
                    foreach (var effect in pool.Value)
                    {
                        Destroy(effect.gameObject);
                    }
                }
            }

            m_CropData = new Dictionary<Vector3Int, CropData>();
            m_GrowingCrops.Clear(); // Clear tracked list
            
            for (int i = 0; i < data.CropDatas.Count; ++i)
            {
                CropData newData = new CropData();
                newData.Load(data.CropDatas[i]);
                
                var pos = data.CropDataPositions[i];
                m_CropData.Add(pos, newData);
                m_GrowingCrops.Add(pos); // Track growing crops
                UpdateCropVisual(pos);
                
                if (!m_HarvestEffectPool.ContainsKey(newData.GrowingCrop))
                {
                    InitHarvestEffect(newData.GrowingCrop);
                }
            }
        }
    }

    [Serializable]
    public struct TerrainDataSave
    {
        public List<Vector3Int> GroundDataPositions;
        public List<TerrainManager.GroundData> GroundDatas;

        public List<Vector3Int> CropDataPositions;
        public List<TerrainManager.CropData.SaveData> CropDatas;
    }
}
