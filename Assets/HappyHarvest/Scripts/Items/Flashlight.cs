using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HappyHarvest
{
    [CreateAssetMenu(fileName = "Flashlight", menuName = "2D Farming/Items/Flashlight")]
    public class Flashlight : Item
    {
        [Tooltip("Light component that will be toggled on/off")]
        public Light2D LightComponent;

        public override bool CanUse(Vector3Int target)
        {
            // Fener her zaman kullanılabilir (hedef gerektirmez)
            return true;
        }

        public override bool Use(Vector3Int target)
        {
            // Fener kullanımı bir şey tüketmez, sadece ışık açık/kapalı olur
            // Bu yüzden false döndürüyoruz (consumable değil)
            return false;
        }

        public override bool NeedTarget()
        {
            // Fener hedef gerektirmez, her zaman kullanılabilir
            return false;
        }

        // Bu metod PlayerController tarafından çağrılacak
        public void ToggleLight(bool isEquipped)
        {
            if (LightComponent != null)
            {
                LightComponent.enabled = isEquipped;
            }
        }
    }
}
