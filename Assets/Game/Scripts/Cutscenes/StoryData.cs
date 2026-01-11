using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStoryData", menuName = "Story/Story Data")]
public class StoryData : ScriptableObject
{
    [Header("Visuals")]
    [Tooltip("Sırasıyla gösterilecek resimler")]
    public List<Sprite> slides = new List<Sprite>();

    [Header("Audio")]
    [Tooltip("Arka planda çalacak müzik (Opsiyonel)")]
    public AudioClip backgroundMusic;
    
    [Tooltip("Müzik ses seviyesi (0-1)")]
    [Range(0f, 1f)]
    public float musicVolume = 1f;

    [Header("Settings")]
    [Tooltip("Resimler arası geçiş süresi (Fade in/out toplamı)")]
    public float transitionDuration = 1.0f;
    
    [Tooltip("Otomatik geçiş olsun mu? (Hayır ise tıklama beklenir)")]
    public bool autoAdvance = false;
    
    [Tooltip("Otomatik geçiş süresi (saniye)")]
    public float slideDisplayTime = 3f;
}
