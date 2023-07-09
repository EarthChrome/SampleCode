using System;
using System.Collections;
using System.Collections.Generic;
using _Template.Scripts.Utils;
using DG.Tweening;
using Lofelt.NiceVibrations;
using MWM.PrototypeTemplate;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VoodooPackages.Tech;

public class ThiefUnitBehavior : UnitBehavior
{
    [SerializeField, BoxGroup("Links")] private Projectile _projectilePrefab;
    [SerializeField, BoxGroup("Links")] private Projectile _projectileReturnPrefab;
    [SerializeField, BoxGroup("Thief"), Range(0.5f, 3f)] private float _returnSpeed;
    [SerializeField, BoxGroup("Settings")]private float _delayShoot;

    private List<GameObject> _enemiesSearchBuffer =  new List<GameObject>();
    private ThiefUnitData _thiefUnitData;
    
    private readonly int _attackAnimKey = Animator.StringToHash("Attack");
    protected IPool<Component> _projectilesReturnPool;
    private bool _isThrowing;
    private float _throwStartTime;


    public override void StartBehavior()
    {
        base.StartBehavior();
        _thiefUnitData = _unitData as ThiefUnitData;
        _projectilesPool = GameGraph.PoolManager.GetPool(_projectilePrefab);
        _projectilesReturnPool = GameGraph.PoolManager.GetPool(_projectileReturnPrefab);
    }

    protected override bool DoAttack()
    {
        if(!MapManager.Instance) return false;
        if (!_hasTarget)
        {
            MapManager.Instance.FindEntities(transform.position, AttackRange * AttackRange, ref _enemiesSearchBuffer);
            if (_enemiesSearchBuffer.Count == 0)
                return false;
            
            Target = _enemiesSearchBuffer[0];
            foreach (GameObject o in _enemiesSearchBuffer)
            {
                if (Vector3.SqrMagnitude(o.transform.position - transform.position) 
                    < Vector3.SqrMagnitude(Target.transform.position - transform.position))
                {
                    Target = o;
                }
            }
            _hasTarget = true;
        }
        if (_hasTarget)
        {
            _hasTarget = false;
            ShootTarget(Target.transform);
            return true;
        }
        return false;
    }

    private void ShootTarget(Transform target)
    {
        _isThrowing = true;
        _throwStartTime = Time.time;
        
        _animator.SetTrigger(_attackAnimKey);
        
        MinionEntity enemy = Target.GetComponent<MinionEntity>();
        var damage = GameGraph.UnitStatsManager.GetUnitDamage(_thiefUnitData, MergeLevel);
        enemy.TakeDamage(_projectileFlightTime+_delayShoot, damage);
        RegisterDamage(damage);
        float returnDuration = _projectileFlightTime / _returnSpeed;
        
        Sequence seq = DOTween.Sequence();
        seq.SetId(gameObject);
        seq.AppendInterval(_delayShoot);
        seq.AppendCallback(() =>
        {
            _isThrowing = false;
            FireProjectile(Target.transform, _projectileFlightTime);
        });
        if(enemy.IsDying)
        {
            seq.AppendInterval(_projectileFlightTime);
            seq.AppendCallback(()=>
            {
                GainBleedResource(returnDuration,target);
            });
            seq.AppendInterval(returnDuration);
            seq.AppendCallback(()=>
            {
                GainResource(_thiefUnitData.ResourceIncome(MergeLevel));
            });
        }
    }
    
    private void GainBleedResource(float returnDuration, Transform target)
    {
        
        Projectile projectile = (Projectile)_projectilesReturnPool.Pick();
        projectile.SetupProjectile(MergeLevel);
        _projectilesReturnPool.ReturnDelayed(projectile, returnDuration+0.5f);
        projectile.GoToTarget(target.position, transform, Vector3.up, returnDuration);
    }
    
    private void GainResource(int amount)
    {
        GameGraph.InGameResource.GainResource(amount);
        FlyingFx fx = GameGraph.PoolManager.DropResourceFXPool.Pick();
        fx.DoFlyToTarget(String.Format("+{0}"+Constants.ResourceSpriteAsset, amount)
            , GameGraph.PoolManager.DropResourceFXPool, transform.position + 2 * Vector3.up, GameGraph.UIManager.ResourceUIPos, 5);
    }
    
    private void OnDestroy()
    {
        DOTween.Kill(gameObject);

        if (_isThrowing)
        {
            FireProjectile(Target.transform, _projectileFlightTime + (Time.time - _throwStartTime));
        }
    }
}
