using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Patches;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Coordinates
{
    public class Coordinates
    {
        private readonly string _coordinatesString;

        private readonly CoordinatesType _coordsType;

        private readonly Vector3 _deltaVectorBig = new Vector3(2f, 2f, 2f);

        private readonly Vector3 _deltaVectorSmall = new Vector3(0.5f, 0.5f, 0.5f);

        private readonly Regex _regexObj;

        private float _latitude;

        private string _latitudeDirection;

        private float _longitude;

        private string _longitudeDirection;

        public Coordinates(string coordinates)
        {
            _coordinatesString = coordinates;

            _regexObj = new Regex(
                @"(?<latitude>\d{1,3}[\.,]\d{2})°*(?<latitude_direction>[NSns])(?:\s+|\s*,\s*)(?<longitude>\d{1,3}[\.,]\d{2})°*(?<longitude_direction>[EWew])");

            _coordsType = CoordinatesType.CoordString;
        }

        public Coordinates(Vector3 coords)
        {
            CoordinatesVector = coords;

            _coordsType = CoordinatesType.CoordVector;
        }

        public Vector3 CoordinatesVector { get; private set; }

        public int FindTile()
        {
            if (_coordsType == CoordinatesType.CoordString)
            {
                if (!ParseCoordinatesString())
                    return Tile.Invalid;

                CoordinatesVector = VectorFromCoordinates();
            }

            var tileId = FindTileByCoords(CoordinatesVector, _deltaVectorSmall);

            return tileId;
        }

        private bool ParseCoordinatesString()
        {
            string longitudeString;
            string latitudeString;
            try
            {
                if (!_regexObj.IsMatch(_coordinatesString))
                    throw new ArgumentException();

                latitudeString = _regexObj.Match(_coordinatesString).Groups["latitude"].Value;
                _latitudeDirection = _regexObj.Match(_coordinatesString).Groups["latitude_direction"].Value;
                longitudeString = _regexObj.Match(_coordinatesString).Groups["longitude"].Value;
                _longitudeDirection = _regexObj.Match(_coordinatesString).Groups["longitude_direction"].Value;
            }
            catch (ArgumentException ex)
            {
                Log.Message($"[GoToTile] Failed match: '{_coordinatesString}'.\n\t{ex}");
                Messages.Message("The string doesn't match a coordinates string.", MessageTypeDefOf.RejectInput);
                return false;
            }

            if (!float.TryParse(longitudeString, out _longitude))
            {
                var message = $"[GoToTile] Failed to parse longitude: '{longitudeString}'.";
                Log.Message(message);
                Messages.Message(message, MessageTypeDefOf.RejectInput);
                return false;
            }

            if (!float.TryParse(latitudeString, out _latitude))
            {
                var message = $"[GoToTile] Failed to parse latitude: '{latitudeString}'.";
                Log.Message(message);
                Messages.Message(message, MessageTypeDefOf.RejectInput);
                return false;
            }

            if (_longitude < 0f || _longitude > 180f)
            {
                var message = $"[GoToTile] longitude should fall in the range [0, 180]: '{_longitude}'.";
                Log.Message(message);
                Messages.Message(message, MessageTypeDefOf.RejectInput);
                return false;
            }

            if (_latitude < 0f || _latitude > 180f)
            {
                var message = $"[GoToTile] latitude should fall in the range [0, 180]: '{_latitude}'.";
                Log.Message(message);
                Messages.Message(message, MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        private float LatitudeFromDirection()
        {
            if (string.Compare(_latitudeDirection, "S", StringComparison.InvariantCultureIgnoreCase) == 0)
                return -_latitude;
            if (string.Compare(_latitudeDirection, "N", StringComparison.InvariantCultureIgnoreCase) == 0)
                return _latitude;

            // shouldn't happen as the regular expression wouldn't match the string
            var message = $"[GoToTile] latitude direction should be N or S but it's '{_latitudeDirection}'.";
            Log.Error(message);
            throw new ArgumentException(message);
        }

        private float LongitudeFromDirection()
        {
            if (string.Compare(_longitudeDirection, "W", StringComparison.InvariantCultureIgnoreCase) == 0)
                return -_longitude;
            if (string.Compare(_longitudeDirection, "E", StringComparison.InvariantCultureIgnoreCase) == 0)
                return _longitude;

            // shouldn't happen as the regular expression wouldn't match the string
            var message = $"[GoToTile] longitude direction should be W or E but it's '{_longitudeDirection}'.";
            Log.Error(message);
            throw new ArgumentException(message);
        }

        private Vector3 VectorFromCoordinates()
        {
            var longitude = LongitudeFromDirection();
            var latitude = LatitudeFromDirection();

            var theta = Mathf.Deg2Rad * longitude; // Azimuth angle (0 <= theta <= 2 * pi)
            var latRad = Mathf.Deg2Rad * latitude;

            var vectorY = Mathf.Sin(latRad);
            var phi = Mathf.Acos(vectorY); // Zenith angle (0 <= phi <= pi)
            var vectorX = Mathf.Sin(phi) * Mathf.Sin(theta);
            var vectorZ = Mathf.Sin(phi) * Mathf.Cos(theta);

            // don't forget that the z component is facing away from us (Left handed coordinates)
            var vec = new Vector3(vectorX, vectorY, -vectorZ);

            // multiply by north pole magnitude (it's the same as multiplying the vector by the radius of the world sphere)
            vec *= Find.WorldGrid.NorthPolePos.magnitude;

            return vec;
        }

        private int FindTileByCoords(Vector3 coords, Vector3 deltaVector)
        {
            var deltaMag = deltaVector.magnitude;

            var roundedVec = coords.Round();
            var deltaVectorMinus = coords - deltaVector;
            var deltaVectorPlus = coords + deltaVector;

            // get all tiles that are roughly at the same latitude
            var trimmedCoords = PatchGenerateGridIntoWorld.TileIdsAndVectors.FindAll(kvp =>
                    kvp.Value.y >= deltaVectorMinus.y && kvp.Value.y <= deltaVectorPlus.y)
                .ToList();

            var foundTile = Tile.Invalid;
            /*
             * very tiny approximation, we round the coordinates vector and try to find the same one
             */
            foreach (var tileVector in trimmedCoords)
            {
                var currentVectorRounded = tileVector.Value.Round();
                if (currentVectorRounded != roundedVec)
                    continue;

                foundTile = tileVector.Key;
                break;
            }
            if (foundTile != Tile.Invalid)
                return foundTile;


            /*
             *  small approximation by using distance from a delta vector
             */
            var vectorMinusDistance = deltaMag;
            foreach (var tileVector in trimmedCoords)
            {
                var dist = Vector3.Distance(tileVector.Value, coords);
                if (!(dist < vectorMinusDistance))
                    continue;

                vectorMinusDistance = dist;
                foundTile = tileVector.Key;
            }
            if (foundTile != Tile.Invalid)
                return foundTile;

            /*
             * larger approximation by using a range
             */
            vectorMinusDistance = deltaMag;
            var vectorMinusTile = Tile.Invalid;
            var vectorPlusDistance = deltaMag;
            var vectorPlusTile = Tile.Invalid;
            foreach (var tileVector in trimmedCoords)
            {
                var deltaMinusDistance = Vector3.Distance(tileVector.Value, deltaVectorMinus);
                var deltaPlusDistance = Vector3.Distance(tileVector.Value, deltaVectorPlus);

                if (deltaMinusDistance < vectorMinusDistance)
                {
                    vectorMinusDistance = deltaMinusDistance;
                    vectorMinusTile = tileVector.Key;
                }

                if (deltaPlusDistance < vectorPlusDistance)
                {
                    vectorPlusDistance = deltaPlusDistance;
                    vectorPlusTile = tileVector.Key;
                }
            }

            var minDistance = Mathf.Min(vectorMinusDistance, vectorPlusDistance);
            if (minDistance < deltaMag)
            {
                if (Math.Abs(minDistance - vectorMinusDistance) < 0.01f)
                    return vectorMinusTile;

                if (Math.Abs(minDistance - vectorPlusDistance) < 0.01f)
                    return vectorPlusTile;
            }

            /*
             * No luck until now, just go there and try by screen coordinates
             */
            var tile = FindTileByScreenPos(coords);

            return tile;
        }

        public static string LongLatOfString(int tileId)
        {
            if (tileId == Tile.Invalid)
                return null;

            var stringBuilder = new StringBuilder();
            var vector = Find.WorldGrid.LongLatOf(tileId);
            stringBuilder.Append(vector.y.ToStringLatitude());
            stringBuilder.Append(" ");
            stringBuilder.Append(vector.x.ToStringLongitude());

            return stringBuilder.ToString();
        }

        public static Vector2 LongLatOf(Vector3 coords)
        {
            var x = Mathf.Atan2(coords.x, -coords.z) * Mathf.Rad2Deg;
            var y = Mathf.Asin(coords.y / 100f) * Mathf.Rad2Deg;
            return new Vector2(x, y);
        }

        public static string LongLatOfString(Vector3 coords)
        {
            var vec2 = LongLatOf(coords);
            return $"{vec2.y.ToStringLatitude()} {vec2.x.ToStringLongitude()}";
        }

        private int FindTileByScreenPos(Vector3 coords)
        {
            Find.WorldCameraDriver.JumpTo(coords);
            var uiPos = GenWorldUI.WorldToUIPosition(coords);
            var tileId = GenWorld.TileAt(uiPos);
            if (tileId == Tile.Invalid)
                return Tile.Invalid;

            // just check that the tile isn't very far from the given coordinates
            var foundTileCoords = Find.WorldGrid.GetTileCenter(tileId);
            var dist = Vector3.Distance(foundTileCoords, coords);
            return dist < _deltaVectorBig.magnitude ? tileId : Tile.Invalid;
        }

        private enum CoordinatesType
        {
            CoordString = 0,
            CoordVector = 1
        }
    }
}
