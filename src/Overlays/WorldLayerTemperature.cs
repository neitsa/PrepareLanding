using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Overlays
{
    public class WorldLayerTemperature : WorldLayer
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();

        protected override float Alpha => 0.99f;

        private Stopwatch sw;

        public WorldLayerTemperature()
        {
            sw = new Stopwatch();
        }

        public override IEnumerable Regenerate()
        {
            sw.Start();

            foreach (var result in base.Regenerate())
                yield return result;

            var temperatureData = PrepareLanding.Instance.GameData.WorldData.TemperatureData;

            // TODO: check if it would be easier to not use the biome here but rather let the TemperatureData expose a set of tiles that has to be drawn (or just their <id, color>)

            // do not draw overlay if not allowed
            if(!temperatureData.AllowDrawOverlay)
                yield break;

            // get current biome
            var biome = temperatureData.Biome;
            if (biome == null)
                yield break;

            // get dictionary <tileId, tileTemperature>
            var tempsByBiome = temperatureData.TemperaturesByBiomes;
            if(!tempsByBiome.ContainsKey(biome))
                yield break;

            var tileDict = tempsByBiome[biome];

            foreach (var tileId in tileDict.Keys)
            {
                if (tileId < 0)
                    continue;

                var material = temperatureData.MaterialFromTileFeature(biome, tileId);

                var subMesh = GetSubMesh(material);
                Find.World.grid.GetTileVertices(tileId, _vertices);

                var startVertIndex = subMesh.verts.Count;
                var currentIndex = 0;
                var maxCount = _vertices.Count;

                while (currentIndex < maxCount)
                {
                    if (currentIndex % 1000 == 0)
                        yield return null;

                    if (subMesh.verts.Count > 60000)
                        subMesh = GetSubMesh(material);

                    // note: no uvs!

                    subMesh.verts.Add(_vertices[currentIndex] + _vertices[currentIndex].normalized * 0.012f);
                    if (currentIndex < maxCount - 2)
                    {
                        subMesh.tris.Add(startVertIndex + currentIndex + 2);
                        subMesh.tris.Add(startVertIndex + currentIndex + 1);
                        subMesh.tris.Add(startVertIndex);
                    }

                    currentIndex++;
                }

                FinalizeMesh(MeshParts.All);
            }

            sw.Stop();
            Log.Message($"[PrepareLanding] Time spent drawing in Regenerate: {sw.Elapsed}");
            sw.Reset();
        }
    }
}