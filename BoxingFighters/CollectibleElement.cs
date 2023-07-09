using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CollectibleElement : CustoElement
{
    public CollectibleDataSO CollectibleData => _collectible;

    public AccessoryDataSO AccessoryData => _accessory;
    public UltimateDataSO UltimateData => _ultimate;
    public GameObject Notif => _notif;

    [SerializeField] private TextMeshProUGUI _elementLevel;
    [SerializeField] private TextMeshProUGUI _elementUpgradeTokens;
    [SerializeField] private TextMeshProUGUI _elementLockedType;
    [SerializeField] private GameObject _usedObject;
    [SerializeField] private GameObject _notif;
    
    [SerializeField] private GameObject _regularElement;
    [SerializeField] private GameObject _emptyElement;
    [SerializeField] private GameObject _lockedElement;

    [SerializeField, BoxGroup("Footer")] private GameObject _footer;
    [SerializeField, BoxGroup("Footer/Slider")] private Slider _tokenSlider;
    [SerializeField, BoxGroup("Footer/Slider")] private Image _tokenFill;
    [SerializeField, BoxGroup("Footer/Slider")] private Image _tokenGlow;
    [SerializeField, BoxGroup("Footer/Slider")] private Image _upgradeIcon;
    
    private CollectibleDataSO _collectible;
    private AccessoryDataSO _accessory;
    private UltimateDataSO _ultimate;
    private Action _onUpgrade;
    
    public void Init(Action action, CollectibleDataSO collectible,bool showUsed, bool showFooter, bool forceNotif)
    {
        
        _footer.SetActive(showFooter);
        Setup(collectible,showUsed, forceNotif);
        _button.onClick.AddListener(() =>
        {
            if (collectible.IsUnlocked)
            {
                action.Invoke();
            }
        }
        );
    }
    
    public void Setup(CollectibleDataSO collectibleDataSo,bool showUsed, bool forceNotif)
    {
        if (showUsed is false)
            _usedObject.SetActive(false);

        _notif.SetActive(forceNotif);

        bool isLocked = !collectibleDataSo.IsUnlocked;
        _lockedElement.SetActive(isLocked);

        if (collectibleDataSo is AccessoryDataSO accessory)
        {
            _accessory = accessory;

            if (isLocked)
            {
                _regularElement.SetActive(false);
                _emptyElement.SetActive(false);
                _elementLockedType.text = _accessory.Type.ToString();
            }
            else
            {
                _regularElement.SetActive(true);
                _emptyElement.SetActive(false);
            }
            
            if (showUsed)
                _usedObject.SetActive(GameGraph.AccessoryManager.Outfit.GetAccessory(accessory.Type).ID == accessory.ID);
        }
        else if (collectibleDataSo is UltimateDataSO ultimate)
        {
            _ultimate = ultimate;
            
            var hideInfo = _ultimate.IsEmptyUltimate;

            if (isLocked)
            {
                _regularElement.SetActive(false);
                _emptyElement.SetActive(false);
                _elementLockedType.text = "ULTIMATE";
            }
            else
            {
                _regularElement.SetActive(!hideInfo);
                _emptyElement.SetActive(hideInfo);
                _lockedElement.SetActive(false);
            }
            
            if (showUsed)
                _usedObject.SetActive(GameGraph.AccessoryManager.Outfit.Ultimates.Contains(ultimate));

            
            _footer.SetActive(!hideInfo);
            
        }

        _collectible = collectibleDataSo;
        _elementIcon.sprite = _collectible.Icon;
        _collectible.Upgrade.OnUpgradeLevelChanged += UpdateCollectibleButton;
        UpdateCollectibleButton(0);
    }

    private void UpdateCollectibleButton(int _)
    {
        _elementName.text = _collectible.DisplayedName;
        _elementLevel.text =( _collectible.Upgrade.Level+1).ToString();

        if (_collectible.Upgrade.IsMaxLevel)
        {
            _upgradeIcon.gameObject.SetActive(false);
            _elementUpgradeTokens.text = "MAX";
            UpdateSlider(new Color(1f, 0.83f, 0.23f), 1f);
        }
        else
        {
            int currentTokens = _collectible.TokenCurrency.Value;
            int requiredTokens = _collectible.Upgrade.CostTokens;
            _elementUpgradeTokens.text = currentTokens+"/"+requiredTokens;

            if (currentTokens>=requiredTokens)
            {
                UpdateSlider(new Color(0.56f, 1f, 0.45f), 1f);
                _upgradeIcon.gameObject.SetActive(true);
            }
            else
            {
                UpdateSlider(new Color(0.24f, 0.87f, 0.92f), (float)currentTokens/requiredTokens);
                _upgradeIcon.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateSlider(Color color, float fill)
    {
        _tokenSlider.value = fill;
        _tokenFill.color = color;
        _tokenGlow.color = color * 1.1f;
    }
    private void OnDestroy()
    {
        if (_collectible!=null)
            _collectible.Upgrade.OnUpgradeLevelChanged -= UpdateCollectibleButton;
    }
}
