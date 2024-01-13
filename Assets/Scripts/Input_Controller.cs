using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Collections;
using System;

public class Input_Controller : MonoBehaviour
{
    [SerializeField] private Camera AR_Camera;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARSession arsession;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private TMP_Text debug_Txt;

    [Header("Interaction Buttons")]
    [SerializeField] private Button DeleteButton;
    [SerializeField] private Button DetailsButton;
    [SerializeField] private Button QuitButton;

    [Header("Chair Buttons")]
    [SerializeField] private Button[] Chair_Button;

    [Header("Chair 3D objects")]
    [SerializeField] private GameObject[] Chair;

    [Header("Product Popup")]
    [SerializeField] private GameObject ProductPopup;
    [SerializeField] private Button ProductCloseButton;
    [SerializeField] private TMP_Text ProductName;
    [SerializeField] private TMP_Text ProductPrice;


    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool isSelected;
    private GameObject selected_chair;
    private GameObject touchedObject;



    private void Start()
    {
        isSelected = false;


        //Adding all button listeners
        DeleteButton.onClick.AddListener(OnDeleteButtonClicked);
        DetailsButton.onClick.AddListener(OnProductButtonClicked);
        QuitButton.onClick.AddListener(OnQuitButtonClicked);
        ProductCloseButton.onClick.AddListener(OnProductCloseClicked);

        Chair_Button[0].onClick.AddListener(()=> SetChairGameobject(Chair[0]));
        Chair_Button[1].onClick.AddListener(() => SetChairGameobject(Chair[1]));
        Chair_Button[2].onClick.AddListener(() => SetChairGameobject(Chair[2]));
        Chair_Button[3].onClick.AddListener(() => SetChairGameobject(Chair[3]));
        Chair_Button[4].onClick.AddListener(() => SetChairGameobject(Chair[4]));
    }


    private void Update()
    {
#if UNITY_EDITOR
        return;
#endif

        // Check if the touch is over a UI element
        if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
        {
            // The touch is over a UI element, do not perform object selection logic
            displayDebug("Pressing UI element");
            return;
        }


        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (isSelected)
                {
                    displayDebug("Displaying selected chair");
                    DisplaySelectedChair();
                }
                else
                {
                    DisableInteractionForAllObjects();
                    displayDebug("Handling touched chair");
                    DisplayUI(false);
                    HandleTouchOnObject();
                }

            }
        }
    }

    //Adding 3D object to environment
    private void DisplaySelectedChair()
    {
        Ray ray = AR_Camera.ScreenPointToRay(Input.GetTouch(0).position);
        if (raycastManager.Raycast(ray, hits))
        {
            Pose pose = hits[0].pose;
            displayDebug("Instantating selected chair");
            Instantiate(selected_chair, pose.position, pose.rotation);
            isSelected = false;
            planeManager.enabled = false;
        }
    }

    //Handling touch interaction of 3D object
    private void HandleTouchOnObject()
    {
        Ray ray = AR_Camera.ScreenPointToRay(Input.GetTouch(0).position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            displayDebug("Getting touched chair object");

            touchedObject = hit.collider.gameObject;

            if (touchedObject != null && touchedObject.CompareTag("Chair"))
            {
                displayDebug("touched chair object is " + touchedObject.name.ToString());
                Chair_Helper helper = touchedObject.GetComponent<Chair_Helper>();
                helper?.EnableDisableInteraction(true);
                DisplayUI(true);
            }
            else
            {
                displayDebug("touched object is none");
                DisableInteractionForAllObjects();
                DisplayUI(false);
            }

            // Disable interaction for other objects
            DisableInteractionForOtherObjects();
        }
    }


    //Disabling Touch interaction for other 3D object except currently touched object 
    private void DisableInteractionForOtherObjects()
    {
        //Collecting list of all chair objects in scene
        GameObject[] allChairs = GameObject.FindGameObjectsWithTag("Chair");

        if (allChairs.Length > 0)
        {
            foreach (GameObject chair in allChairs)
            {
                // Skip the touched object
                if (chair != touchedObject)
                {
                    Chair_Helper otherChairHelper = chair.GetComponent<Chair_Helper>();

                    if (otherChairHelper != null)
                    {
                        //displayDebug("Getting disable for  " + chair.name.ToString());
                        otherChairHelper.EnableDisableInteraction(false);
                    }
                }
            }
        }
    }


    //Disabling Touch interaction for all 3D objects
    private void DisableInteractionForAllObjects()
    {
        //Collecting list of all chair objects in scene
        GameObject[] allChairs = GameObject.FindGameObjectsWithTag("Chair");

        if (allChairs.Length > 0)
        {
            foreach (GameObject chair in allChairs)
            {
                Chair_Helper otherChairHelper = chair.GetComponent<Chair_Helper>();
                if (otherChairHelper != null)
                {
                    //displayDebug("Getting disable for  " + chair.name.ToString());
                    otherChairHelper.EnableDisableInteraction(false);
                }
            }
        }
    }


    //Button Action after selecting chair
    public void SetChairGameobject(GameObject chair)
    {
        DisableInteractionForAllObjects();
        DisplayUI(false);
        isSelected = true;
        selected_chair = chair;
    }


    //Debug Text to look into screen
    private void displayDebug(string debug)
    {
        Debug.Log(debug);

        //Uncomment below line to show debug on screen
        //debug_Txt.text = debug;
    }


    //Delete button action for selected 3D object
    public void OnDeleteButtonClicked()
    {

        displayDebug("Delete button pressed ");

        if (touchedObject != null)
        {
            displayDebug("Deleting object " + touchedObject.name);
            Destroy(touchedObject);
        }
        else
        {
            displayDebug("No object to delete");
        }
    }

    //Product button action for selected 3D object
    private void OnProductButtonClicked()
    {
        displayDebug("Product Details button pressed ");
        if (touchedObject != null)
        {
            displayDebug("Product Details fetching for " + touchedObject.name);
            string fetchedchair = touchedObject.name.Replace("(Clone)", "");
            StartCoroutine(FetchChairDetails(fetchedchair));
        }
        else
        {
            displayDebug("No object to fetch product details");
        }
    }

    //Fetching Product details dynamically from JSON file hosted in Github
    private IEnumerator FetchChairDetails(string chairName)
    {
        string apiUrl = "https://raw.githubusercontent.com/Senthilrakhul95/productdetails/main/productprice.json";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(apiUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + webRequest.error);
            }
            else
            {
                Root chairData = JsonUtility.FromJson<Root>(webRequest.downloadHandler.text);

                // Find the chair details based on the chairName
                ChairData chairDetails = chairData.chairs.Find(chair => chair.name == chairName);

                if (chairDetails != null)
                {
                    // Display the chair details
                    Debug.Log("Chair Price for " + chairDetails.name + " is: " + chairDetails.price);
                    DisplayUI(false);
                    ProductPopup.SetActive(true);
                    ProductName.text = chairDetails.name;
                    ProductPrice.text = chairDetails.price;
                }
                else
                {
                    Debug.LogWarning("Chair details not found for " + chairName);
                    ProductPopup.SetActive(true);
                    ProductName.text = "Not found";
                    ProductPrice.text = "Not found";
                }
            }
        }
    }

    //Product popup close button action
    private void OnProductCloseClicked()
    {
        ProductPopup.SetActive(false);
        ProductName.text = "";
        ProductPrice.text = "";
        DisableInteractionForAllObjects();
    }


    //Quit button action
    private void OnQuitButtonClicked()
    {
        // Quit the application
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    //Function to Show/Hide product and delete button
    private void DisplayUI(bool display)
    {
        DeleteButton.gameObject.SetActive(display);
        DetailsButton.gameObject.SetActive(display);
    }



    [Serializable]
    public class ChairData
    {
        public string name;
        public string price;
    }
    [Serializable]
    public class Root
    {
        public List<ChairData> chairs;
    }

}



