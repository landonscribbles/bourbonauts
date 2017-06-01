using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using TDTK;

namespace TDTK {
	
	public class PlatformTD : MonoBehaviour {

        // Temporary start and end points OLD
        public Vector3 creepStartPoint;
        public Vector3 creepEndPoint;
        //

        // Temp start and endpoints NEW
        public GameObject creepGridStartPoint;
        public GameObject creepGridEndPoint;
        //

        float xGridOffset;
        float zGridOffset;

        //prefabID of tower available to this platform
        //prior to runtime, this stores the ID of all the unavailable tower on the list, it gets reverse in VerifyTowers (call by BuildManager)
        public List<int> availableTowerIDList=new List<int>();

        // OLD NEW ADDITIONS
        private List<GameObject> gridWaypoints = new List<GameObject>();
        private List<GameObject> openGridWaypoints = new List<GameObject>();
        private List<GameObject> pathThroughGrid = new List<GameObject>();
        //

        // NEW NEW ADDITIONS (Yep)
        private List<List<GameObject>> gridPositions = new List<List<GameObject>>();
        private List<List<GameObject>> openGridPositions = new List<List<GameObject>>();
        //




        private BuildManager buildManager = null;
		
		[HideInInspector] public GameObject thisObj;
		[HideInInspector] public Transform thisT;
		
		public void Init(){
			thisObj=gameObject;
			thisT=transform;
			thisObj.layer=LayerManager.LayerPlatform();
		}
		
		public void VerifyTowers(List<UnitTower> towerList){
			List<int> newList=new List<int>();
			for(int i=0; i<towerList.Count; i++){
				if(!availableTowerIDList.Contains(towerList[i].prefabID)) newList.Add(towerList[i].prefabID);
			}
			availableTowerIDList=newList;
		}
        
        // NEW
        public void BuildGridPointList() {
            // For pathing build this out as a 2D list with 0 index of the list being the bottom corner
            // then for pathfinding it's a matter of checking to see if there is a list: prior to, after
            // and then within it's own list prior to and after the current index (position is "X", checked is "c"):
            // [*][*][*]
            // [*][*][*]
            // [*][c][*]
            // [c][X][c]
            // [*][c][*]


            if (buildManager == null) {
                buildManager = GameObject.Find("BuildManager").GetComponent<BuildManager>();
            }
            float xGridSize = (Utility.GetWorldScale(transform).x) / buildManager.gridSize;
            float zGridSize = (Utility.GetWorldScale(transform).z) / buildManager.gridSize;

            Bounds platformBounds = gameObject.GetComponent<Renderer>().bounds;
            xGridOffset = platformBounds.extents.x * 2 / xGridSize;
            zGridOffset = platformBounds.extents.z * 2 / zGridSize;
            float platformBasePosX = transform.position.x - platformBounds.extents.x;
            float platformBasePosZ = transform.position.z - platformBounds.extents.z;

            for (int i=0; i < xGridSize; i++) {
                for (int j=0; j < zGridSize; j++) {
                    // NEW HOTNESS
                    if (gridPositions.Count != i) {
                        gridPositions.Add(new List<GameObject>());
                    }
                    Vector3 gridPos = new Vector3(xGridOffset * i + platformBasePosX + (xGridOffset / 2), transform.position.y, zGridOffset * j + platformBasePosZ + (zGridOffset / 2));
                    GameObject gridWaypoint = new GameObject("PlatformWaypoint");
                    gridWaypoint.transform.position = gridPos;
                    gridWaypoint.transform.parent = transform;
                    gridPositions[i].Add(gridWaypoint);
                    //gridWaypoints.Add(gridWaypoint);
                    //

                    // OLD COPIED CODE
                    //Vector3 gridPos = new Vector3(xGridOffset * i + platformBasePosX + (xGridOffset / 2), transform.position.y, zGridOffset * j + platformBasePosZ + (zGridOffset / 2));
                    //GameObject gridWaypoint = new GameObject("PlatformWaypoint");
                    //gridWaypoint.transform.position = gridPos;
                    //gridWaypoint.transform.parent = transform;
                    //gridWaypoints.Add(gridWaypoint);
                    //
                }
            }
        }

        public List<GameObject> GetCreepPath() {
            GetOpenWaypoints();
            CalculateCreepPath();
            return pathThroughGrid;
        }

        // Need to get the start and end path for this
        private void CalculateCreepPath() {
            // Let's start with Dijkstra, we can move to A* if this ends
            // up being too much of a performance hit

            // This also could be adjusted to assume all movement tile costs are the same, though depending on how we do towers
            // This may end up being useful for mobs to avoid things like slows

            // Assume we're only working on a 2D plane for pathing

            // OLD COPIED CODE
            Dictionary<Vector2, int> unvisitedPoints = new Dictionary<Vector2, int>();
            Dictionary<Vector2, int> visitedPoints = new Dictionary<Vector2, int>();
            Dictionary<Vector2, Vector2> traveledPath = new Dictionary<Vector2, Vector2>();
            //

            // NEW HOTNESS
            Dictionary<GameObject, int> unvisitedGridPositions = new Dictionary<GameObject, int>();
            Dictionary<GameObject, int> visitedGridPositions = new Dictionary<GameObject, int>();
            Dictionary<GameObject, GameObject> traveledGridPath = new Dictionary<GameObject, GameObject>();
            //

            // OLD COPIED CODE
            for (int i=0; i < openGridWaypoints.Count; i++) {
                Vector2 waypoint2D = new Vector2(openGridWaypoints[i].transform.position.x, openGridWaypoints[i].transform.position.z);
                unvisitedPoints[waypoint2D] = 10000;
                traveledPath[waypoint2D] = new Vector2(-10000, -10000);
            }
            //

            // NEW HOTNESS
            for (int i=0; i < openGridPositions.Count; i++) {
                for (int j=0; j < openGridPositions[i].Count; j++) {
                    unvisitedGridPositions[openGridPositions[i][j]] = 10000;
                    traveledGridPath[openGridPositions[i][j]] = null;
                }
            }
            //

            // OLD COPIED CODE
            Vector2 currentPoint = new Vector2(creepStartPoint.x, creepStartPoint.z);
            //int currentDistance = 0;
            unvisitedPoints[currentPoint] = currentDistance;
            //

            // NEW HOTNESS
            GameObject currentGridPosition = creepGridStartPoint;
            int currentDistance = 0;
            unvisitedGridPositions[currentGridPosition] = currentDistance;
            //

            // OLD COPIED CODE
            while (true) {
                // Need neighbors here
                Dictionary<Vector2, int> neighbors = GetWaypointNeighbors(creepStartPoint);
                List<Vector2> unvisitedWaypoints = unvisitedPoints.Keys.ToList();
                // Needs to be changed from a foreach, foreach is bad in unity
                foreach (KeyValuePair<Vector2, int> neighbor in neighbors) {
                    if (!unvisitedWaypoints.Contains(neighbor.Key)) {
                        continue;
                    }
                    int newDistance = currentDistance + neighbor.Value;
                    if (unvisitedPoints[neighbor.Key] > newDistance) {
                        unvisitedPoints[neighbor.Key] = newDistance;
                        traveledPath[neighbor.Key] = currentPoint;
                    }
                }

                visitedPoints[currentPoint] = currentDistance;
                unvisitedPoints.Remove(currentPoint);
                if (unvisitedPoints.Count == 0) {
                    break;
                }
                List<KeyValuePair<Vector2, int>> orderedCandidates = unvisitedPoints.ToList().OrderBy(o => o.Value).ToList();
                KeyValuePair<Vector2, int> nextNode = orderedCandidates[0];
                currentPoint = nextNode.Key;
                currentDistance = nextNode.Value;
            }
            //

            // NEW HOTNESS
            while (true) {
                // Need to have an i and j index for iterating to pass into GetGridNeighbors (that still needs to be created!)
                HERE
                Dictionary<GameObject, int> gridNeighbors = GetGridNeighbors(i, j)
            }
            //

            List<KeyValuePair<Vector2, int>> shortPathDistance = new List<KeyValuePair<Vector2, int>>();
            List<Vector2> shortPath = new List<Vector2>();

            Vector2 previousPoint = new Vector2(creepEndPoint.x, creepEndPoint.z);
            Vector2 startPoint = new Vector2(creepStartPoint.x, creepStartPoint.z);

            while (previousPoint != startPoint) {
                KeyValuePair<Vector2, int> pointAndCost = new KeyValuePair<Vector2, int>(previousPoint, visitedPoints[previousPoint]);
                shortPathDistance.Add(pointAndCost);
                previousPoint = traveledPath[previousPoint];
            }

            shortPathDistance.Reverse();

            // Convert shortPathDistance back into gameObject locations

        }

        private Dictionary<Vector2, int> GetWaypointNeighbors(Vector2 startPoint) {
            // Remeber startPoint.y == Vector3 gridWaypoint.transform.position.z

            Dictionary<Vector2, int> neighbors = new Dictionary<Vector2, int>();
            Vector2 potentialNeighbor0 = new Vector2(startPoint.x + xGridOffset, startPoint.y);
            Vector2 potentialNeighbor1 = new Vector2(startPoint.x - xGridOffset, startPoint.y);
            Vector2 potentialNeighbor2 = new Vector2(startPoint.x, startPoint.y + zGridOffset);
            Vector2 potentialNeighbor3 = new Vector2(startPoint.x, startPoint.y - zGridOffset);
            for (int i=0; i < openGridWaypoints.Count; i++) {
                if (openGridWaypoints[i].transform.position.x == potentialNeighbor0.x && openGridWaypoints[i].transform.position.z == potentialNeighbor0.y) {
                    neighbors.Add(potentialNeighbor0, 1);
                    continue;
                }
                else if (openGridWaypoints[i].transform.position.x == potentialNeighbor1.x && openGridWaypoints[i].transform.position.z == potentialNeighbor1.y) {
                    neighbors.Add(potentialNeighbor1, 1);
                    continue;
                }
                else if (openGridWaypoints[i].transform.position.x == potentialNeighbor2.x && openGridWaypoints[i].transform.position.z == potentialNeighbor2.y) {
                    neighbors.Add(potentialNeighbor2, 1);
                    continue;
                }
                else if (openGridWaypoints[i].transform.position.x == potentialNeighbor3.x && openGridWaypoints[i].transform.position.z == potentialNeighbor3.y) {
                    neighbors.Add(potentialNeighbor3, 1);
                    continue;
                }
            }
            return neighbors;
        }

        private void GetOpenWaypoints() {
            openGridWaypoints.Clear();

            // Pulled from buildmanager
            //layerMask for platform only
            LayerMask maskPlatform = 1 << LayerManager.LayerPlatform();
            //layerMask for detect all collider within buildPoint
            LayerMask maskAll = 1 << LayerManager.LayerPlatform();
            int terrainLayer = LayerManager.LayerTerrain();
            if (terrainLayer >= 0) maskAll |= 1 << terrainLayer;


            // NEW HOTNESS
            for (int i=0; i < gridPositions.Count; i++) {
                for (int j=0; j < gridPositions[i].Count; j++) {
                    if (openGridPositions.Count != i) {
                        openGridPositions.Add(new List<GameObject>());
                    }
                    Collider[] collisions = Physics.OverlapSphere(gridPositions[i][j].transform.position, buildManager.gridSize / 2 * 0.9f, ~maskAll);
                    if (collisions.Length > 0) {
                        // If platforms/terrain/etc are on the wrong layer
                        // we'll get false positives for "used" waypoints
                        openGridPositions[i].Add(null);
                        continue;
                    }
                    openGridPositions[i].Add(gridPositions[i][j]);
                }
            }
            //

            // OLD COPIED CODE
            //for (int i=0; i < gridWaypoints.Count; i++) {
            //    Collider[] collisions = Physics.OverlapSphere(gridWaypoints[i].transform.position, buildManager.gridSize / 2 * 0.9f, ~maskAll);
            //    if (collisions.Length>0) {
            //        // If platforms/terrain/etc are on the wrong layer
            //        // we'll get false positives for "used" waypoints
            //        continue;
            //    }
            //    openGridWaypoints.Add(gridWaypoints[i]);
            //}
            //
        }

        // Iterate through all points check collision for a tower, list of
        // open waypoints from the collision check
        // END NEW
		
	}
	

}