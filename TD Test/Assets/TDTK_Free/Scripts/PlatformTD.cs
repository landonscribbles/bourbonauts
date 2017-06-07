using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using TDTK;

namespace TDTK {
	
	public class PlatformTD : MonoBehaviour {

        // Temp start and endpoints NEW and testing
        public GameObject creepGridStartPoint = null;
        public GameObject creepGridEndPoint = null;
        //

        float xGridOffset;
        float zGridOffset;

        //prefabID of tower available to this platform
        //prior to runtime, this stores the ID of all the unavailable tower on the list, it gets reverse in VerifyTowers (call by BuildManager)
        public List<int> availableTowerIDList=new List<int>();


        // NEW NEW ADDITIONS (Yep)
        private List<List<GameObject>> gridPositions = new List<List<GameObject>>();
        private List<List<GameObject>> openGridPositions = new List<List<GameObject>>();
        private List<GameObject> pathThroughGrid = new List<GameObject>();
        //

        struct GridPos {
            public int x;
            public int y;

            public GridPos(int x, int y) {
                this.x = x;
                this.y = y;
            }
        }


        private BuildManager buildManager = null;
		
		[HideInInspector] public GameObject thisObj;
		[HideInInspector] public Transform thisT;
		
		public void Init(){
			thisObj=gameObject;
			thisT=transform;
			thisObj.layer=LayerManager.LayerPlatform();
		}

        public void CalculateCreepEntryPoint(Vector3 entryPosition) {
            creepGridStartPoint = GetClosestWaypointToPoint(entryPosition);
        }

        public void CalculateCreepExitPoint(Vector3 exitPosition) {
            creepGridEndPoint = GetClosestWaypointToPoint(exitPosition);
        }

        private GameObject GetClosestWaypointToPoint(Vector3 externalPoint) {
            if (gridPositions.Count == 0) {
                BuildGridPointList();
            }
            GameObject closestWaypoint = gridPositions[0][0];
            float closestWaypointDistance = Vector3.Distance(closestWaypoint.transform.position, externalPoint);
            for (int i = 0; i < gridPositions.Count; i++) {
                for (int j = 0; j < gridPositions[i].Count; j++) {
                    float waypointDistance = Vector3.Distance(gridPositions[i][j].transform.position, externalPoint);
                    if (waypointDistance < closestWaypointDistance) {
                        closestWaypoint = gridPositions[i][j];
                        closestWaypointDistance = waypointDistance;
                    }
                }
            }
            return closestWaypoint;
        }

        public void OnDrawGizmos() {
            if (creepGridStartPoint == null) {
                return;
            }

            Vector3 startAndEndCubeSize = new Vector3(.3f, .3f, .3f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(creepGridStartPoint.transform.position, startAndEndCubeSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(creepGridEndPoint.transform.position, startAndEndCubeSize);
            for (int i = 0; i < gridPositions.Count; i++) {
                for (int j = 0; j < gridPositions[i].Count; j++) {
                    DrawDebugWaypointCube(gridPositions[i][j].transform.position);
                }
            }

            for (int i = 0; i < pathThroughGrid.Count; i++) {
                DrawDebugCreepPath(pathThroughGrid[i].transform.position);
            }

        }

        private void DrawDebugWaypointCube(Vector3 cubePos) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(cubePos, new Vector3(.5f, .5f, .5f));
        }

        private void DrawDebugCreepPath(Vector3 pathPointPos) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pathPointPos, .05f);
        }

        public void VerifyTowers(List<UnitTower> towerList){
			List<int> newList=new List<int>();
			for(int i=0; i<towerList.Count; i++){
				if(!availableTowerIDList.Contains(towerList[i].prefabID)) newList.Add(towerList[i].prefabID);
			}
			availableTowerIDList=newList;
		}

        public List<GameObject> GetCreepPath() {
            BuildGridPointList();
            GetOpenWaypoints();
            CalculateCreepPath();
            return pathThroughGrid;
        }

        // NEW
        public void BuildGridPointList() {
            if (gridPositions.Count > 0) {
                return;
            }
            Debug.Log("Building grid point list");
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
                    if (gridPositions.Count == i) {
                        gridPositions.Add(new List<GameObject>());
                    }
                    Vector3 gridPos = new Vector3(xGridOffset * i + platformBasePosX + (xGridOffset / 2), transform.position.y, zGridOffset * j + platformBasePosZ + (zGridOffset / 2));
                    GameObject gridWaypoint = new GameObject("PlatformWaypoint");
                    gridWaypoint.transform.position = gridPos;
                    gridWaypoint.transform.parent = transform;
                    gridPositions[i].Add(gridWaypoint);
                }
            }
        }

        // Need to get the start and end path for this
        private void CalculateCreepPath() {
            // Let's start with Dijkstra

            // This also could be adjusted to assume all movement tile costs are the same, though depending on how we do towers
            // This may end up being useful for mobs to avoid things like slows

            // Assume we're only working on a 2D plane for pathing

            // NEW HOTNESS
            Dictionary<GameObject, int> unvisitedGridPositions = new Dictionary<GameObject, int>();
            Dictionary<GameObject, int> visitedGridPositions = new Dictionary<GameObject, int>();
            Dictionary<GameObject, GameObject> traveledGridPath = new Dictionary<GameObject, GameObject>();
            //

            // NEW HOTNESS
            for (int i=0; i < openGridPositions.Count; i++) {
                for (int j=0; j < openGridPositions[i].Count; j++) {
                    if (openGridPositions[i][j] == null) {
                        continue;
                    }
                    unvisitedGridPositions[openGridPositions[i][j]] = 10000;
                    traveledGridPath[openGridPositions[i][j]] = null;
                }
            }
            //


            // NEW HOTNESS
            GameObject currentGridPosition = creepGridStartPoint;
            int currentDistance = 0;
            unvisitedGridPositions[currentGridPosition] = currentDistance;
            //

            // NEW HOTNESS
            while (true) {
                // Need to have an i and j index for iterating to pass into GetGridNeighbors (that still needs to be created!)

                Dictionary<GameObject, int> gridNeighbors = GetGridNeighbors(currentGridPosition);
                List<GameObject> unvisitedGridWaypoints = unvisitedGridPositions.Keys.ToList();
                // Need to change from foreach because unity performance of this is bad
                foreach (KeyValuePair<GameObject, int> neighbor in gridNeighbors) {
                    if (!unvisitedGridWaypoints.Contains(neighbor.Key)) {
                        continue;
                    }
                    int newDistance = currentDistance + neighbor.Value;
                    if (unvisitedGridPositions[neighbor.Key] > newDistance) {
                        unvisitedGridPositions[neighbor.Key] = newDistance;
                        traveledGridPath[neighbor.Key] = currentGridPosition;
                    }
                }
                visitedGridPositions[currentGridPosition] = currentDistance;
                unvisitedGridPositions.Remove(currentGridPosition);
                if (unvisitedGridPositions.Count == 0) {
                    break;
                }
                List<KeyValuePair<GameObject, int>> orderedGridCandidates = unvisitedGridPositions.ToList().OrderBy(o => o.Value).ToList();
                KeyValuePair<GameObject, int> nextGridNode = orderedGridCandidates[0];
                currentGridPosition = nextGridNode.Key;
                currentDistance = nextGridNode.Value;
            }
            //

            // NEW HOTNESS
            List<KeyValuePair<GameObject, int>> shortGridPathDistance = new List<KeyValuePair<GameObject, int>>();
            List<GameObject> shortGridPath = new List<GameObject>();

            GameObject previousGridPoint = creepGridEndPoint;
            GameObject startGridPoint = creepGridStartPoint;

            while (previousGridPoint != startGridPoint) {

                // NEW HOTNESS
                if (previousGridPoint == null) {
                    break;
                }
                shortGridPath.Add(previousGridPoint);
                previousGridPoint = traveledGridPath[previousGridPoint];
                //
            }
            shortGridPath.Add(startGridPoint);
            shortGridPath.Reverse();
            pathThroughGrid = shortGridPath;
            //

        }

        private Dictionary<GameObject, int> GetGridNeighbors(GameObject gridPosition) {
            GridPos gridPos = GetWaypointGridPosition(gridPosition);
            int xIdx = gridPos.x;
            int yIdx = gridPos.y;
            Dictionary<GameObject, int> neighbors = new Dictionary<GameObject, int>();
            if (xIdx != 0 && xIdx != gridPositions.Count - 1) {
                if (yIdx != 0 && yIdx != gridPositions[xIdx].Count - 1) {
                    if (openGridPositions[xIdx - 1][yIdx]!= null) neighbors.Add(gridPositions[xIdx - 1][yIdx], 1);
                    if (openGridPositions[xIdx + 1][yIdx] != null) neighbors.Add(gridPositions[xIdx + 1][yIdx], 1);
                    if (openGridPositions[xIdx][yIdx - 1] != null) neighbors.Add(gridPositions[xIdx][yIdx - 1], 1);
                    if (openGridPositions[xIdx][yIdx + 1] != null) neighbors.Add(gridPositions[xIdx][yIdx + 1], 1);
                } else if (yIdx == 0 && yIdx == gridPositions[xIdx].Count - 1) {
                    if (openGridPositions[xIdx - 1][yIdx] != null) neighbors.Add(gridPositions[xIdx - 1][yIdx], 1);
                    if (openGridPositions[xIdx + 1][yIdx] != null) neighbors.Add(gridPositions[xIdx + 1][yIdx], 1);
                } else if (yIdx == 0) {
                    if (openGridPositions[xIdx - 1][yIdx] != null) neighbors.Add(gridPositions[xIdx - 1][yIdx], 1);
                    if (openGridPositions[xIdx + 1][yIdx] != null) neighbors.Add(gridPositions[xIdx + 1][yIdx], 1);
                    if (openGridPositions[xIdx][yIdx + 1] != null) neighbors.Add(gridPositions[xIdx][yIdx + 1], 1);
                } else {
                    if (openGridPositions[xIdx - 1][yIdx] != null) neighbors.Add(gridPositions[xIdx - 1][yIdx], 1);
                    if (openGridPositions[xIdx + 1][yIdx] != null) neighbors.Add(gridPositions[xIdx + 1][yIdx], 1);
                    if (openGridPositions[xIdx][yIdx - 1] != null) neighbors.Add(gridPositions[xIdx][yIdx - 1], 1);
                }
            } else if (xIdx == 0 && xIdx == gridPositions.Count - 1) {
                if (yIdx != 0 && yIdx != gridPositions[xIdx].Count - 1) {
                    if (openGridPositions[xIdx][yIdx - 1] != null) neighbors.Add(gridPositions[xIdx][yIdx - 1], 1);
                    if (openGridPositions[xIdx][yIdx + 1] != null) neighbors.Add(gridPositions[xIdx][yIdx + 1], 1);
                } else if (yIdx == 0 && yIdx == gridPositions[xIdx].Count - 1) {
                } else if (yIdx == 0) {
                    if (openGridPositions[xIdx][yIdx + 1] != null) neighbors.Add(gridPositions[xIdx][yIdx + 1], 1);
                } else {
                    if (openGridPositions[xIdx][yIdx - 1] != null) neighbors.Add(gridPositions[xIdx][yIdx - 1], 1);
                }
            } else if (xIdx == 0) {
                if (yIdx != 0 && yIdx != gridPositions[xIdx].Count - 1) {
                    if (openGridPositions[xIdx + 1][yIdx] != null) neighbors.Add(gridPositions[xIdx + 1][yIdx], 1);
                    if (openGridPositions[xIdx][yIdx - 1] != null) neighbors.Add(gridPositions[xIdx][yIdx - 1], 1);
                    if (openGridPositions[xIdx][yIdx + 1] != null) neighbors.Add(gridPositions[xIdx][yIdx + 1], 1);
                } else if (yIdx == 0 && yIdx == gridPositions[xIdx].Count - 1) {
                    if (openGridPositions[xIdx + 1][yIdx] != null) neighbors.Add(gridPositions[xIdx + 1][yIdx], 1);
                } else if (yIdx == 0) {
                    if (openGridPositions[xIdx + 1][yIdx] != null) neighbors.Add(gridPositions[xIdx + 1][yIdx], 1);
                    if (openGridPositions[xIdx][yIdx + 1] != null) neighbors.Add(gridPositions[xIdx][yIdx + 1], 1);
                } else {
                    if (openGridPositions[xIdx + 1][yIdx] != null) neighbors.Add(gridPositions[xIdx + 1][yIdx], 1);
                    if (openGridPositions[xIdx][yIdx - 1] != null) neighbors.Add(gridPositions[xIdx][yIdx - 1], 1);
                }
                
            } else {
                if (yIdx != 0 && yIdx != gridPositions[xIdx].Count - 1) {
                    if (openGridPositions[xIdx - 1][yIdx] != null) neighbors.Add(gridPositions[xIdx - 1][yIdx], 1);
                    if (openGridPositions[xIdx][yIdx - 1] != null) neighbors.Add(gridPositions[xIdx][yIdx - 1], 1);
                    if (openGridPositions[xIdx][yIdx + 1] != null) neighbors.Add(gridPositions[xIdx][yIdx + 1], 1);
                } else if (yIdx == 0 && yIdx == gridPositions[xIdx].Count - 1) {
                    if (openGridPositions[xIdx - 1][yIdx] != null) neighbors.Add(gridPositions[xIdx - 1][yIdx], 1);
                } else if (yIdx == 0) {
                    if (openGridPositions[xIdx - 1][yIdx] != null) neighbors.Add(gridPositions[xIdx - 1][yIdx], 1);
                    if (openGridPositions[xIdx][yIdx + 1] != null) neighbors.Add(gridPositions[xIdx][yIdx + 1], 1);
                } else {
                    if (openGridPositions[xIdx - 1][yIdx] != null) neighbors.Add(gridPositions[xIdx - 1][yIdx], 1);
                    if (openGridPositions[xIdx][yIdx - 1] != null) neighbors.Add(gridPositions[xIdx][yIdx - 1], 1);
                }
            }
            return neighbors;
        }

        private GridPos GetWaypointGridPosition(GameObject waypoint) {
            GridPos gridPos = new GridPos(-1, -1);
            for (int i = 0; i < gridPositions.Count; i++) {
                for (int j = 0; j < gridPositions[i].Count; j++) {
                    if (waypoint == gridPositions[i][j]) {
                        gridPos.x = i;
                        gridPos.y = j;
                        break;
                    }
                }
            }
            return gridPos;
        }

        private void GetOpenWaypoints() {
            openGridPositions.Clear();
            // Pulled from buildmanager
            //layerMask for platform only
            LayerMask maskPlatform = 1 << LayerManager.LayerPlatform();
            //layerMask for detect all collider within buildPoint
            LayerMask maskAll = 1 << LayerManager.LayerPlatform();
            int terrainLayer = LayerManager.LayerTerrain();
            if (terrainLayer >= 0) maskAll |= 1 << terrainLayer;
            maskAll |= 1 << LayerManager.LayerCreep();


            // NEW HOTNESS
            for (int i=0; i < gridPositions.Count; i++) {
                for (int j=0; j < gridPositions[i].Count; j++) {
                    if (openGridPositions.Count == i) {
                        openGridPositions.Add(new List<GameObject>());
                    }
                    Collider[] collisions = Physics.OverlapSphere(gridPositions[i][j].transform.position, buildManager.gridSize / 2 * 0.9f, ~maskAll);
                    if (collisions.Length > 0) {
                        // If platforms/terrain/etc are on the wrong layer
                        // we'll get false positives for "used" waypoints
                        Debug.Log("Collided with num objects: " + collisions.Length);
                        Debug.Log("Collided object: " + collisions[0].gameObject.name);
                        Debug.Log("Adding blocked tile at gridPos[" + i +"][" + j +"]");
                        openGridPositions[i].Add(null);
                        continue;
                    }
                    openGridPositions[i].Add(gridPositions[i][j]);
                }
            }
            //
        }

        // END NEW
		
	}
	

}