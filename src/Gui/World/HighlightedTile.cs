using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Gui.World
{
    public class HighlightedTile
    {
        private enum AlphaRampDirection
        {
            AlphaRampUp,
            AlphaRampDown
        }

        private static readonly List<Vector3> TmpVerts = new List<Vector3>();

        private static readonly List<int> TmpIndices = new List<int>();

        private Mesh _mesh;

        public float ColorPct;

        public Material CustomMat;

        public string DisplayString;

        public int Tile;

        private float _alpha;
        private const float TickAlphaQuantum = 1f / 90f;
        private AlphaRampDirection _alphaRampDirection = AlphaRampDirection.AlphaRampDown;

        public HighlightedTile(int tile, float colorPct = 0f, string text = null)
        {
            Tile = tile;
            ColorPct = colorPct;
            DisplayString = text;
        }

        public HighlightedTile(int tile, Material mat, string text = null)
        {
            Tile = tile;
            CustomMat = mat;
            DisplayString = text;
        }

        private Vector2 ScreenPos
        {
            get
            {
                var tileCenter = Find.WorldGrid.GetTileCenter(Tile);
                return GenWorldUI.WorldToUIPosition(tileCenter);
            }
        }

        private bool VisibleForCamera
        {
            get
            {
                var rect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
                return rect.Contains(ScreenPos);
            }
        }

        public float DistanceToCamera
        {
            get
            {
                var tileCenter = Find.WorldGrid.GetTileCenter(Tile);
                return Vector3.Distance(Find.WorldCamera.transform.position, tileCenter);
            }
        }

        public void Draw()
        {
            // do not draw tiles that are not visible, this should help on very large planets
            if (!VisibleForCamera)
                return;
            
            // if no tile mesh, build it
            if (_mesh == null)
            {
                Find.WorldGrid.GetTileVertices(Tile, TmpVerts);
                for (var i = 0; i < TmpVerts.Count; i++)
                {
                    var tmpVert = TmpVerts[i];
                    TmpVerts[i] = tmpVert + tmpVert.normalized * 0.012f;
                }
                _mesh = new Mesh {name = "HighlightedTile"};
                _mesh.SetVertices(TmpVerts);
                TmpIndices.Clear();
                for (var j = 0; j < TmpVerts.Count - 2; j++)
                {
                    TmpIndices.Add(j + 2);
                    TmpIndices.Add(j + 1);
                    TmpIndices.Add(0);
                }
                _mesh.SetTriangles(TmpIndices, 0);
            }

            Material material;
            if (CustomMat != null)
            {
                material = CustomMat;

                TileBreath(material);
            }
            else
            {
                var num = Mathf.RoundToInt(ColorPct * 100f);
                num %= 100;
                material = WorldDebugMatsSpectrum.Mat(num);
            }

            Graphics.DrawMesh(_mesh, Vector3.zero, Quaternion.identity, material, WorldCameraManager.WorldLayer);
        }

        public void OnGui()
        {
            //note: this function is called only when we are very close to the world surface

            if (!VisibleForCamera)
                return;

            var screenPos = ScreenPos;
            var rect = new Rect(screenPos.x - 20f, screenPos.y - 20f, 40f, 40f);
            if (DisplayString != null)
                Verse.Widgets.Label(rect, DisplayString);

        }

        protected virtual void TileBreath(Material material)
        {
            if (_alpha < float.Epsilon)
            {
                _alpha = 0f;
                _alphaRampDirection = AlphaRampDirection.AlphaRampUp;
            }
            if (_alpha >= 1f)
            {
                _alpha = 1f;
                _alphaRampDirection = AlphaRampDirection.AlphaRampDown;
            }

            var matColor = material.color;
            matColor.a = _alpha;
            material.color = matColor;

            if(_alphaRampDirection == AlphaRampDirection.AlphaRampDown)
                _alpha -= TickAlphaQuantum;
            else
                _alpha += TickAlphaQuantum;
        }
    }
}