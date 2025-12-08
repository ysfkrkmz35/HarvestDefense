using UnityEngine;
using UnityEngine.Tilemaps; // Eðer Tilemap kullanýyorsan gereklidir, yoksa silinebilir.

public class BuildingSystem : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject buildingPrefab; // Ýnþa edilecek gerçek bina
    public GameObject ghostObject;    // Þeffaf önizleme objesi
    public LayerMask groundLayer;     // Zemin katmaný (Raycast için)
    public LayerMask obstacleLayer;   // Engel katmaný (Üst üste bina kurmamak için)

    [Header("Ekonomi (Örnek)")]
    public int buildingCost = 100;
    public int currentMoney = 500; // Bunu normalde GameManager'dan çekersin

    private Camera _mainCamera;
    private SpriteRenderer _ghostRenderer;
    private bool _isBuildingMode = true; // Ýnþaat modu açýk mý?

    void Start()
    {
        _mainCamera = Camera.main;
        _ghostRenderer = ghostObject.GetComponent<SpriteRenderer>();
        ghostObject.SetActive(false); // Baþlangýçta gizle
    }

    void Update()
    {
        // Ýnþaat modunu 'B' tuþu ile açýp kapatma (Örnek)
        if (Input.GetKeyDown(KeyCode.B)) ToggleBuildingMode();

        if (_isBuildingMode)
        {
            HandleBuilding();
        }
    }

    void HandleBuilding()
    {
        // 1. Grid Snapping: Farenin dünyadaki pozisyonunu al ve yuvarla
        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0; // 2D olduðu için Z'yi sýfýrla

        // Mathf.Floor kullanarak tam sayý karelere oturtuyoruz
        // Örn: (3.7, 5.2) -> (3, 5) olur.
        // Eðer binanýn merkezi ortadaysa +0.5f eklemek gerekebilir: new Vector3(Mathf.Floor(x) + 0.5f, ...)
        // Bu kod objeyi karenin ortasýna (3.5, 4.5 gibi) taþýr
        Vector3 snappedPos = new Vector3(Mathf.Floor(mousePos.x) + 0.5f, Mathf.Floor(mousePos.y) + 0.5f, 0);

        // Ghost objeyi ýzgaraya oturt
        ghostObject.transform.position = snappedPos;
        ghostObject.SetActive(true);

        // 2. Placement Logic & Ghost Rengi
        bool canBuild = CanBuild(snappedPos);

        // Renk deðiþimi: Yapýlabilirse YEÞÝL, deðilse KIRMIZI
        if (canBuild)
            _ghostRenderer.color = new Color(0, 1, 0, 0.5f); // Yarý saydam yeþil
        else
            _ghostRenderer.color = new Color(1, 0, 0, 0.5f); // Yarý saydam kýrmýzý

        // 3. Týklama ve Ýnþa Etme
        if (Input.GetMouseButtonDown(0) && canBuild)
        {
            PlaceBuilding(snappedPos);
        }
    }
    bool CanBuild(Vector3 targetPos)
    {
        // 1. Para Kontrolü
        if (currentMoney < buildingCost) return false;

        // 2. Engel Kontrolü (DÜZELTÝLDÝ)
        // Artýk +0.5f EKLEMÝYORUZ. Çünkü targetPos zaten tam ortasý.
        Collider2D hit = Physics2D.OverlapBox(targetPos, new Vector2(0.9f, 0.9f), 0, obstacleLayer);

        if (hit != null)
        {
            // Debug.Log("Engel var: " + hit.name); // Ýstersen konsoldan neye çarptýðýný görebilirsin
            return false;
        }

        // 3. Zemin Kontrolü (DÜZELTÝLDÝ)
        // Buradan da ofseti kaldýrdýk.
        RaycastHit2D groundHit = Physics2D.Raycast(targetPos, Vector2.zero, 0f, groundLayer);

        if (groundHit.collider == null) return false;

        return true;
    }

    void PlaceBuilding(Vector3 position)
    {
        // Artýk position zaten ortalanmýþ geliyor, +0.5f 'leri siliyoruz!
        Instantiate(buildingPrefab, position, Quaternion.identity);

        currentMoney -= buildingCost;
        Debug.Log("Bina inþa edildi! Kalan Para: " + currentMoney);
    }

    void ToggleBuildingMode()
    {
        _isBuildingMode = !_isBuildingMode;
        ghostObject.SetActive(_isBuildingMode);
    }
}