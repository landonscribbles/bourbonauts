using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK {
	
	public class PathTD : MonoBehaviour {

		
		public List<Transform> wpList=new List<Transform>();
        private List<Transform> wpInnerList = new List<Transform>();

        public bool createPathLine=true;
		
		public float dynamicOffset=1;
		
		public bool loop=false;
		public int loopPoint=0;

        // NEW
        bool linePathInited = false;
        List<GameObject> creepLinePath;
        List<GameObject> creepLinePoints;
        GameObject linePathParent;
        // Added a static instance reference to update the path in buildmanager
        public static PathTD instance;
        //


        public void Init(){
            instance = this;
			
			if(loop){
                // Looping not updated with new changes
				loopPoint=Mathf.Min(wpList.Count-1, loopPoint); //looping must start 1 waypoint before the destination
			}
			
			//~ if(createPathLine) StartCoroutine(CreatePathLine());
		}

        public void UpdateWaypointList() {
            wpInnerList.Clear();
            for (int i = 0; i < wpList.Count; i++) {
                PlatformTD platformTD = wpList[i].gameObject.GetComponent<PlatformTD>();
                if (platformTD == null) {
                    wpInnerList.Add(wpList[i].transform);
                } else {
                    // This will blow up if there isn't waypoint prior to and after the platform entry
                    platformTD.CalculateCreepEntryPoint(wpList[i - 1].position);
                    platformTD.CalculateCreepExitPoint(wpList[i + 1].position);
                    List<GameObject> platformWPList = platformTD.GetCreepPath();
                    for (int j = 0; j < platformWPList.Count; j++) {
                        wpInnerList.Add(platformWPList[j].transform);
                    }
                    // - Give platformTD the waypoint prior to this to figure out the entry point
                    // - Get the next point after the current to get the exit
                    // - Add the platform waypoints to the waypoint list
                }
            }
            if (!linePathInited) {
                InitPath();
            }
            UpdatePathLine();
        }

        public List<Vector3> GetWaypointList() {
            // Check if platform here
            List<Vector3> list = new List<Vector3>();            
            for (int i = 0; i < wpInnerList.Count; i++) {
                list.Add(wpInnerList[i].position);
            }
            if (!linePathInited) {
                InitPath();
                UpdateWaypointList();
            }
            return list;
        }
		
		public int GetPathWPCount(){ return wpInnerList.Count; }
		public Transform GetSpawnPoint(){
            if (wpInnerList.Count == 0) {
                UpdateWaypointList();
            }
            return wpInnerList[0];
        }
		
		public int GetLoopPoint(){ return loopPoint; }
		
		
		public float GetPathDistance(int wpID=1){
			if(wpInnerList.Count==0) return 0;
			
			float totalDistance=0;
			
			for(int i=wpID; i< wpInnerList.Count; i++)
				totalDistance+=Vector3.Distance(wpInnerList[i-1].position, wpInnerList[i].position);
			
			return totalDistance;
		}
		
		void Start(){

            // NEW
            if (createPathLine) {
                if (!linePathInited) {
                    InitPath();
                }
                UpdatePathLine();
            }
            //
        }

        // NEW
        private void InitPath() {
            linePathInited = true;
            creepLinePath = new List<GameObject>();
            creepLinePoints = new List<GameObject>();
            linePathParent = new GameObject();
            linePathParent.transform.position = transform.position;
            linePathParent.transform.parent = transform;
            linePathParent.name = "PathLine";
        }
        

        private void UpdatePathLine() {
            for (int i=0; i < creepLinePath.Count; i++ ) {
                Destroy(creepLinePath[i]);
            }
            creepLinePath.Clear();
            for (int i = 0; i < creepLinePoints.Count; i++) {
                Destroy(creepLinePoints[i]);
            }
            creepLinePoints.Clear();

            GameObject pathLine = (GameObject)Resources.Load("ScenePrefab/PathLine");
            GameObject pathPoint = (GameObject)Resources.Load("ScenePrefab/PathPoint");

            Vector3 startPoint = Vector3.zero;
            Vector3 endPoint = Vector3.zero;

            for (int i=0; i < wpInnerList.Count; i++) {
                GameObject point = (GameObject)Instantiate(pathPoint, wpInnerList[i].position, Quaternion.identity);
                point.transform.parent = linePathParent.transform;
                creepLinePoints.Add(point);

                endPoint = wpInnerList[i].position;

                if(i>0) {
                    GameObject lineObj = (GameObject)Instantiate(pathLine, startPoint, Quaternion.identity);
                    creepLinePath.Add(lineObj);
                    LineRenderer lineRen = lineObj.GetComponent<LineRenderer>();
                    lineRen.SetPosition(0, startPoint);
                    lineRen.SetPosition(1, endPoint);

                    lineObj.transform.parent = linePathParent.transform;
                }

                startPoint = wpInnerList[i].position;
            }
        }

        public bool showGizmo=true;
		public Color gizmoColor=Color.blue;
		void OnDrawGizmos(){
			if(showGizmo){
				Gizmos.color = gizmoColor;
				
				//~ if(Application.isPlaying){
					//~ for(int i=1; i<wpSectionList.Count; i++){
						//~ List<Vector3> subPathO=GetWPSectionPath(i-1);
						//~ List<Vector3> subPath=GetWPSectionPath(i);
						
						//~ //Debug.Log(i+"    "+wpSectionList[i].isPlatform+"    "+subPathO.Count+"   "+subPath.Count);
						
						//~ Gizmos.DrawLine(subPathO[subPathO.Count-1], subPath[0]);
						//~ for(int n=1; n<subPath.Count; n++){
							//~ Gizmos.DrawLine(subPath[n-1], subPath[n]);
						//~ }
					//~ }
				//~ }
				//~ else{
					for(int i=1; i< wpInnerList.Count; i++){
						if(wpInnerList[i-1]!=null && wpInnerList[i]!=null)
							Gizmos.DrawLine(wpInnerList[i-1].position, wpInnerList[i].position);
					}
				//~ }
			}
		}
		
	}
	
}



