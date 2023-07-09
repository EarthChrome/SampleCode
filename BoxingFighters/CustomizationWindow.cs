using System;
using System.Collections.Generic;
using System.Linq;
using _Template.Scripts.UI.TabBar;
using MWM.PrototypeTemplate.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CustomizationWindow : TabBarWindowImpl
{
    [SerializeField, BoxGroup("Internal Links")] private GameObject _gridButtons;
    [SerializeField, BoxGroup("Internal Links")] private GameObject _gridElementsAccessories;
    [SerializeField, BoxGroup("Internal Links")] private GameObject _gridElementsSkin;
    [SerializeField, BoxGroup("Internal Links")] private GameObject _accessorySelectionScreen;
    [SerializeField, BoxGroup("Internal Links")] private GameObject _skinSelectionScreen;
    [SerializeField, BoxGroup("Internal Links")] private ButtonUI _accessorySelectionBackButton;
    [SerializeField, BoxGroup("Internal Links")] private ButtonUI _skinSelectionBackButton;
    [SerializeField, BoxGroup("Internal Links")] private ButtonUI _boostersButton;
    [SerializeField, BoxGroup("Internal Links")] private GameObject _stats;

    [FormerlySerializedAs("_customizationElementPrefab"),SerializeField, BoxGroup("Asset Links")] private SkinElement _skinElementPrefab;
    [FormerlySerializedAs("_customizationElementPrefab"),SerializeField, BoxGroup("Asset Links")] private CollectibleElement _collectibleElementPrefab;
    [SerializeField, BoxGroup("Asset Links")] private CollectiblePopup _accessoryPopup;
    [SerializeField, BoxGroup("Asset Links")] private CollectiblePopup _ultimatePopup;
    [SerializeField, BoxGroup("Asset Links")] private BoostersPopup _boostersPopup;

    [SerializeField, BoxGroup("Internal Links")] private TextMeshProUGUI _collectibleType;


    [SerializeField, BoxGroup("Internal Links")] private StatElement _statHealth;
    [SerializeField, BoxGroup("Internal Links")] private StatElement _statAttack;
    [SerializeField, BoxGroup("Internal Links")] private StatElement _statDefense;
    [SerializeField, BoxGroup("Internal Links")] private StatElement _statSpeed;

    [SerializeField, BoxGroup("Links")] private GameObject _notifHome;

    private List<CustoElement> _buttons;
    private List<CustoElement> _buttonsSkins;

    // TODO @clement Remove shortcuts if not used in more places
    private CollectibleElement _buttonHead => GetButtonForAccessoryType(AccessoryDataSO.AccessoryType.Head);
    private CollectibleElement _buttonClothes => GetButtonForAccessoryType(AccessoryDataSO.AccessoryType.Clothes);
    private CollectibleElement _buttonHands => GetButtonForAccessoryType(AccessoryDataSO.AccessoryType.Hands);
    private CollectibleElement _buttonFeet => GetButtonForAccessoryType(AccessoryDataSO.AccessoryType.Feet);

    public VcamProfileTypeCusto GetCameraType(AccessoryDataSO.AccessoryType type)
    {
        return type switch
        {
            AccessoryDataSO.AccessoryType.Head => VcamProfileTypeCusto.VCAM_CUSTO_HEAD,
            AccessoryDataSO.AccessoryType.Clothes => VcamProfileTypeCusto.VCAM_CUSTO_CLOTHES,
            AccessoryDataSO.AccessoryType.Hands => VcamProfileTypeCusto.VCAM_CUSTO_HANDS,
            AccessoryDataSO.AccessoryType.Feet => VcamProfileTypeCusto.VCAM_CUSTO_FEET,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public override void Init()
    {
        base.Init();
        _accessoryPopup.Init();
        _ultimatePopup.Init();
        _boostersPopup.Init();
        
        _accessorySelectionBackButton.AddListener(CloseCustoSelector);
        _skinSelectionBackButton.AddListener(CloseSkinSelector);
        _boostersButton.AddListener(DisplayBoostersPopup);
        _accessorySelectionScreen.gameObject.SetActive(false);
        _skinSelectionScreen.gameObject.SetActive(false);
        CreateButtons();
        _buttonsSkins = new List<CustoElement>();
        GameGraph.CoinsCurrency.OnValueChanged += UpdateNotifications;
        void UpdateNotifications(int _)
        {
            UpdateNotificationStates();
            RefreshAccessoryButtons();
        }

        GameGraph.UIManager.RewardsRecapWindow.OnVisibilityStatusChanged += window => UpdateNotificationStates();
        UpdateNotificationStates();
    }

    private void CreateButtons()
    {
        _buttons = new List<CustoElement>();

        CreateButtonSkin();

        CreateButtonUltimate(0);
        CreateButtonUltimate(1);
        CreateButtonUltimate(2);
        CreateButton(AccessoryDataSO.AccessoryType.Head);
        CreateButton(AccessoryDataSO.AccessoryType.Clothes);
        CreateButton(AccessoryDataSO.AccessoryType.Hands);
        CreateButton(AccessoryDataSO.AccessoryType.Feet);

        _statHealth.Setup(BoxerStatType.HEALTH);
        _statAttack.Setup(BoxerStatType.ATTACK);
        _statDefense.Setup(BoxerStatType.DEFENSE);
        _statSpeed.Setup(BoxerStatType.SPEED);
    }

    private void UpdateStats()
    {
        _statHealth.UpdateValue();
        _statAttack.UpdateValue();
        _statDefense.UpdateValue();
        _statSpeed.UpdateValue();
    }

    private void CreateButtonSkin()
    {
        var newButton = Instantiate(_skinElementPrefab, _gridButtons.transform);
        newButton.Init(OpenSkinSelector,GameGraph.AccessoryManager.Outfit.Skin, false);
        _buttons.Add(newButton);
    }

    private void CreateButton(AccessoryDataSO.AccessoryType type)
    {
        var newButton = Instantiate(_collectibleElementPrefab, _gridButtons.transform);
        newButton.Init(()=>OpenAccessorySelector(newButton),
            GameGraph.AccessoryManager.GetCurrentAccessory(type),false,true,
            GameGraph.AccessoryManager.ShouldDisplayNotif(type) );
        _buttons.Add(newButton);
    }

    private GameObject CreateButtonUltimate(int slot)
    {
        var ultimate = GameGraph.AccessoryManager.Outfit.Ultimates[slot];

        
        var newButton = Instantiate(_collectibleElementPrefab, _gridButtons.transform);
        newButton.Init(() => OpenUltimateSelector(newButton, slot)
            ,ultimate, false,!ultimate.IsEmptyUltimate, GameGraph.AccessoryManager.ShouldDisplayNotifUltimates() );
        _buttons.Add(newButton);

        return newButton.Notif;
    }

    private void RefreshAccessoryButtons()
    {
        SetupAccessoryButton(_buttonHead, AccessoryDataSO.AccessoryType.Head);
        SetupAccessoryButton(_buttonClothes, AccessoryDataSO.AccessoryType.Clothes);
        SetupAccessoryButton(_buttonHands, AccessoryDataSO.AccessoryType.Hands);
        SetupAccessoryButton(_buttonFeet, AccessoryDataSO.AccessoryType.Feet);

        var ultimateButtons = _buttons.Where(element => element is CollectibleElement collectibleElement && collectibleElement.UltimateData!=null).ToArray();
        ((CollectibleElement)ultimateButtons[0]).Setup(GameGraph.AccessoryManager.Outfit.Ultimates[0], false,GameGraph.AccessoryManager.ShouldDisplayNotifUltimates());
        ((CollectibleElement)ultimateButtons[1]).Setup(GameGraph.AccessoryManager.Outfit.Ultimates[1], false,GameGraph.AccessoryManager.ShouldDisplayNotifUltimates());
        ((CollectibleElement)ultimateButtons[2]).Setup(GameGraph.AccessoryManager.Outfit.Ultimates[2], false, GameGraph.AccessoryManager.ShouldDisplayNotifUltimates());
    }

    private void SetupAccessoryButton(CollectibleElement button, AccessoryDataSO.AccessoryType type)
    {
        var currentAccessory = GameGraph.AccessoryManager.GetCurrentAccessory(type);
        button.Setup(currentAccessory, false, GameGraph.AccessoryManager.ShouldDisplayNotif(type));
    }

    private void OpenSkinSelector(SkinDataSO skinDataSo)
    {
        _stats.SetActive(false);
        GameGraph.UIManager.TabBarMenu.Close();
        _skinSelectionScreen.SetActive(true);
        _gridButtons.SetActive(false);
        _buttonsSkins.Clear();

        GameGraph.CameraManager.ToggleCustoCam(VcamProfileTypeCusto.VCAM_CUSTO_SKIN);
       
        RefreshSkinsList();
    }

    private void RefreshSkinsList()
    {
        _gridElementsSkin.SetActive(true);
        foreach (Transform o in _gridElementsSkin.transform)
        {
            Destroy(o.gameObject);
        }
        
        var skinsToShow =GameGraph.AccessoryManager.CustoDataSet.Skins;

        foreach (var skin in skinsToShow)
        {
            var element = Instantiate(_skinElementPrefab, _gridElementsSkin.transform);
            element.Init(SkinElementClicked, skin,true);
            _buttonsSkins.Add(element);
        }
    }

    private void OpenUltimateSelector(CollectibleElement _,int slot)
    {
        OpenCustoSelector();
        _collectibleType.text = "ULTIMATES";
        
        RefreshUltimatesList(slot);
    }

    private void RefreshUltimatesList(int slot)
    {
        var ultimatesToShow =GameGraph.AccessoryManager.UltimatesDataSet.Ultimates.Where(so => so.IsEmptyUltimate==false);
        foreach (var ultimate in ultimatesToShow)
        {
            var element = Instantiate(_collectibleElementPrefab, _gridElementsAccessories.transform);
            element.Init(()=>DisplayUltimatePopup(element,slot), ultimate, true, true,false);
        }
    }
    
    private void CloseSkinSelector()
    {
        GameGraph.UIManager.TabBarMenu.Open();
        _stats.SetActive(true);

        _skinSelectionScreen.SetActive(false);
        _gridButtons.SetActive(true);
        UpdateStats();
        RefreshAccessoryButtons();
        GameGraph.CameraManager.ToggleCustoCam(VcamProfileTypeCusto.VCAM_CUSTO_BASE);

    }

    private void OpenAccessorySelector(CollectibleElement collectible)
    {
        OpenCustoSelector();

        _collectibleType.text = collectible.AccessoryData.Type.ToString();
        RefreshAccessoryList(collectible.AccessoryData.Type);
    }

    private void RefreshAccessoryList(AccessoryDataSO.AccessoryType type)
    {
        var accessoriesToShow =GameGraph.AccessoryManager.GetAccessoriesOfType(type);
        foreach (var accessory in accessoriesToShow)
        {
            var element = Instantiate(_collectibleElementPrefab, _gridElementsAccessories.transform);
            element.Init(()=>DisplayAccessoryPopup(element), accessory, true,true,false);
        }
    }

    private void OpenCustoSelector()
    {
        _stats.SetActive(false);
        _accessorySelectionScreen.SetActive(true);
        _gridButtons.SetActive(false);
        _gridElementsAccessories.SetActive(true);
        
        foreach (Transform o in _gridElementsAccessories.transform)
        {
            Destroy(o.gameObject);
        }
    }

    private void CloseCustoSelector()
    {
        _stats.SetActive(true);

        _accessorySelectionScreen.SetActive(false);
        _gridButtons.SetActive(true);
        UpdateStats();
        RefreshAccessoryButtons();
    }

    private void CustomizationElementClicked(AccessoryDataSO accessory)
    {
        GetButtonForAccessoryType(accessory.Type).Setup(accessory, false, GameGraph.AccessoryManager.ShouldDisplayNotif(accessory.Type));
        GameGraph.AccessoryManager.ChangeAccessory(accessory);
        GameGraph.AccessoryDataManager.SaveData();
        GameGraph.CameraManager.ToggleCustoCam(VcamProfileTypeCusto.VCAM_CUSTO_BASE );
        OpenCustoSelector();
        RefreshAccessoryList(accessory.Type);
    }
    
    private void UltimateElementClicked(UltimateDataSO ultimate, int slot)
    {
        ((CollectibleElement)(_buttons.Where(element => element is CollectibleElement collectibleElement && collectibleElement.UltimateData != null)).ToArray()[slot]).Setup(ultimate, false, 
            GameGraph.AccessoryManager.ShouldDisplayNotifUltimates());
        GameGraph.AccessoryManager.ChangeUltimate(ultimate, slot);
        GameGraph.AccessoryDataManager.SaveData();
        GameGraph.CameraManager.ToggleCustoCam(VcamProfileTypeCusto.VCAM_CUSTO_BASE);
        OpenCustoSelector();
        RefreshUltimatesList(slot);
    }

    private void SkinElementClicked(SkinDataSO skin)
    {
        ((SkinElement)(_buttons.First(element => element is SkinElement skinElement && skinElement.SkinDataSo != null))).Setup(skin);
        GameGraph.AccessoryManager.ChangeSkin(skin);
        GameGraph.AccessoryDataManager.SaveData();
        foreach (var custoElement1 in _buttonsSkins)
        {
            var custoElement = (SkinElement)custoElement1;
            custoElement.RefreshElement();
        }
    }
    private CollectibleElement GetButtonForAccessoryType(AccessoryDataSO.AccessoryType type)
    {
        return _buttons.First(element =>element is CollectibleElement collectibleElement && collectibleElement.AccessoryData !=null && collectibleElement.AccessoryData.Type == type) as CollectibleElement;
    }

    private void DisplayAccessoryPopup(CollectibleElement accessory)
    {
        _accessoryPopup.SetupAndOpen((acc) =>
            {
                CustomizationElementClicked(acc as AccessoryDataSO);
            }, accessory);
    }

    private void DisplayUltimatePopup(CollectibleElement ultimate, int slot)
    {
        _ultimatePopup.SetupAndOpen((ult) =>
        {
            UltimateElementClicked(ult as UltimateDataSO, slot);
        }, ultimate);
    }

    public void DisplayBoostersPopup()
    {
        _boostersPopup.Open();
    }

    protected override void OpenInternal()
    {
        base.OpenInternal();
        RefreshAccessoryButtons();
        UpdateStats();
        UpdateNotificationStates();

    }
    
    private void UpdateNotificationStates()
    {
        var notifHead = GameGraph.AccessoryManager.ShouldDisplayNotif(AccessoryDataSO.AccessoryType.Head);
        var notifClothes =GameGraph.AccessoryManager.ShouldDisplayNotif(AccessoryDataSO.AccessoryType.Clothes);
        var notifHands =GameGraph.AccessoryManager.ShouldDisplayNotif(AccessoryDataSO.AccessoryType.Hands);
        var notifFeet =GameGraph.AccessoryManager.ShouldDisplayNotif(AccessoryDataSO.AccessoryType.Feet);
        var notifUltimates =GameGraph.AccessoryManager.ShouldDisplayNotifUltimates();
        bool notifHome = notifHead || notifClothes || notifHands || notifFeet || notifUltimates;

        _notifHome.SetActive(notifHome);
    }

    protected override void CloseInternal()
    {
        base.CloseInternal();
    }
}