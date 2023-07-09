using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

// This should be renamed to something else. Map manager ?
public class MapSpawner : MonoBehaviour
{
    [SerializeField, BoxGroup("Level Tiles"),InlineButton("GetMapTiles")] private List<Tile> MapTiles;
    public EnemyWave[] EnemyWaves => _enemyWaves;
    [SerializeField, Required,BoxGroup("@MapStats"), InlineButton("GetWaves")]private EnemyWave[] _enemyWaves;

    [SerializeField, BoxGroup("Links"), Required] private NavMeshSurface _navMesh;

    public void Init()
    {
        _navMesh.BuildNavMesh();
        GetWaves();
        GetMapTiles();
        GameGraph.TiledBoardManager.AddLevelTiles(MapTiles);
        foreach (var mapTile in MapTiles)
        {
            if (mapTile.WaveIndex==0) continue;

            mapTile.Disappear();
        }
        GameGraph.EnemyWavesManager.OnWaveOver += CheckDynamicTiles;
    }
    
    public void CheckDynamicTiles(int totalWaves, int waveIndex)
    {
        if (waveIndex == 0)
            return;

        var mapTiles = MapTiles.Where(x => x.WaveIndex == waveIndex).ToList();
        foreach (var mapTile in mapTiles)
        {
            mapTile.DoAnim();
        }
    }


    private void GetWaves()
    {
        _enemyWaves = GetComponentsInChildren<EnemyWave>();
    }

    private void GetMapTiles()
    {
        MapTiles = GetComponentsInChildren<Tile>().ToList();
    }

    private void OnDestroy()
    {
        GameGraph.EnemyWavesManager.OnWaveOver -= CheckDynamicTiles;
    }

#if UNITY_EDITOR

    private void DrawFlag(Vector3 position)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(position, Vector3.one*5);
        Gizmos.DrawLine(position,position+Vector3.up*10f );
        Gizmos.DrawLine(position+Vector3.up*10f,position+Vector3.up*8f+Vector3.left*4f );
        Gizmos.DrawLine(position+Vector3.up*6f, position+Vector3.up*8f+Vector3.left*4f );
    }
    
    public string MapStats
    {
        get
        {
            if (_enemyWaves!=null && _enemyWaves.Length>0)
            {
                int totalXp = 0, totalEnergy = 0, totalEnemies = 0;
                foreach (var enemyWave in _enemyWaves)
                {
                    if (enemyWave == null)
                    {
                        return "[!] Warning, a wave is not assigned";
                        continue;
                    }
                    enemyWave.ComputeData();
                    totalXp +=  Mathf.RoundToInt(enemyWave.TotalXP);
                    totalEnergy +=  Mathf.RoundToInt(enemyWave.TotalEnergy);
                    totalEnemies +=  enemyWave.TotalEnemies;
                }

                return "Waves: "+_enemyWaves.Length+" | Energy: " + totalEnergy + " | XP : " + totalXp + " | Enemies : " + totalEnemies;
            }
            else
            {
                return "[!] Warning, no wave is set";
            }
        }
    }
#endif
}
