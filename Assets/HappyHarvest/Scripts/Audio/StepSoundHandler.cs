using System;
using System.Collections;
using System.Collections.Generic;
using HappyHarvest;
using Template2DCommon;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace HappyHarvest
{
    /// <summary>
    /// Handle playing a random step sound during walk animation. This need to be on the same GameObject of the player
    /// with the Animator as it need to receive the PlayStepSound events from the walking animation.
    /// Contains a list of pairing of list of tiles to list of audio clip, so it can play different clip based on the
    /// tile under the player.
    /// Note : the Tilemap that is checked for which tile is walked on need a WalkableSurface component on it.
    /// </summary>
    public class StepSoundHandler : MonoBehaviour
    {
        [Serializable]
        public class TileSoundMapping
        {
            public TileBase[] Tiles;
            public AudioClip[] StepSounds;
        }

        public AudioClip[] DefaultStepSounds;
        public TileSoundMapping[] SoundMappings;

        private Dictionary<TileBase, AudioClip[]> m_Mapping = new();

        void Start()
        {
            foreach (var mapping in SoundMappings)
            {
                foreach (var tile in mapping.Tiles)
                {
                    m_Mapping[tile] = mapping.StepSounds;
                }
            }
        }

        //This is called by animation event on the walking animation of the character.
        public void PlayStepSound()
        {
            // FIX: Added safety check. If GameManager or the Tilemap is missing, fall back to default sound.
            if (GameManager.Instance == null || GameManager.Instance.WalkSurfaceTilemap == null)
            {
                PlayDefaultSound();
                return;
            }

            var underCell = GameManager.Instance.WalkSurfaceTilemap.WorldToCell(transform.position);
            var tile = GameManager.Instance.WalkSurfaceTilemap.GetTile(underCell);

            SoundManager.Instance.PlaySFXAt(transform.position,
                (tile != null && m_Mapping.ContainsKey(tile))
                    ? GetRandomEntry(m_Mapping[tile])
                    : GetRandomEntry(DefaultStepSounds), false);
        }

        // Helper function to keep code clean
        void PlayDefaultSound()
        {
            if (DefaultStepSounds != null && DefaultStepSounds.Length > 0 && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFXAt(transform.position, GetRandomEntry(DefaultStepSounds), false);
            }
        }

        AudioClip GetRandomEntry(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }
}