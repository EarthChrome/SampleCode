using System;
using System.Collections.Generic;
using MWM.PrototypeTemplate.Currency;
using MWM.PrototypeTemplate.UI;
using UnityEngine;

public class RewardChest : IReward
{
    public List<IReward> Rewards => _rewards;

    private readonly RewardType _type;
    private List<IReward> _rewards;
    private RewardData _data;

    public RewardChest(RewardType type,List<IReward> rewards, RewardData data)
    {
        _type = type;
        _rewards = rewards;
        _data = data;
    }

    public void SetupRewardStartValue()
    {
        foreach (IReward iReward in _rewards)
        {
            iReward.SetupRewardStartValue();
        }
    }
    
    public RewardData GetData()
    {
        return _data;
    }
    public void GainReward()
    {
        GameEventsManager.InvokeGameEvent(1, GameEventsManager.OpenChest, "");
        foreach (var reward in _rewards) reward.GainReward();
    }

    public void GainBonus()
    {
        foreach (var reward in _rewards) reward.GainBonus();
    }

    public void Multiply(float multiplier)
    {
        throw new NotImplementedException();
    }
    public bool IsChest()
    {
        return true;
    }

    public int GetStartValue()
    {
        return 0;
    }
    public int GetQuantity()
    {
        return 1;
    }
    public int GetBonus()
    {
        return 0;
    }

    public float GetMultiplier()
    {
        return 1f;
    }

    public RewardType GetRewardType()
    {
        return _type;
    }
    public Sprite GetIcon()
    {
        return _data.IconCard;
    }

    public Sprite GetIconLarge()
    {
        return _data.IconCard;
    }
    public new string ToString()
    {
        string content = _data.DisplayName + " : " + _rewards.Count + " rewards inside";
 
        foreach (var reward in _rewards)
        {
            content += "\n    ";
            content += reward.ToString();
        }

        return content;
    }
}
