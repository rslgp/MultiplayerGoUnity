using System;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

public class MultiplayerController : MonoBehaviour
{
    private static MultiplayerController _instance;

    // This property provides a global point of access to the Singleton instance.
    public static MultiplayerController Instance
    {
        get
        {
            if (_instance == null)
            {
                // If the instance doesn't exist, create it.
                _instance = FindObjectOfType<MultiplayerController>();

                if (_instance == null)
                {
                    // If there are no instances in the scene, create an empty GameObject with the Singleton script.
                    GameObject singletonObject = new GameObject(typeof(MultiplayerController).Name);
                    _instance = singletonObject.AddComponent<MultiplayerController>();
                }
            }

            return _instance;
        }
    }

    // Make sure to prevent the Singleton from being instantiated manually.
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this as MultiplayerController;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //ONE CONNECTION PER INSTANCE
    private TcpClient client;
    private NetworkStream stream;
    private byte[] receiveBuffer = new byte[1024];

    public int playerNum;   
    [SerializeField]
    public float moveSpeed=1.0f;
    [SerializeField]
    public Transform[] playersTransform;

    // Create a dictionary to store movement data for each player
    private Dictionary<int, int> playerMovements = new Dictionary<int, int>();



    // Start is called before the first frame update
    private void Start()
    {
        try
        {
            //client = new TcpClient("game.055190.xyz", 8080); // Connect to the Go server on localhost and port 8080
            client = new TcpClient("10.126.33.168", 8080); // Connect to the Go server 
            stream = client.GetStream();

            // Send a message to the server
            string message = "Hello from Unity client\n";
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);

            //int bytesRead = stream.Read(receiveBuffer, 0, receiveBuffer.Length);
            //string response = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
            //Debug.Log("Received: " + response);

            //try{
            //    playerNum = System.Convert.ToInt32(response);
            //}catch(Exception e){
            //    Debug.Log("erro convert");
            //}
            

            // Start a coroutine to continuously read messages from the server
            StartCoroutine(ReadMessages());
        }
        catch (Exception e)
        {
            Debug.LogError("Socket error: " + e.ToString());
        }
    }


    public static ConcurrentQueue<string> QueueMultithread = new ConcurrentQueue<string>();

    private int receivedPlayer=0;
    private int receivedDirection=0;
    private IEnumerator ReadMessages()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.001f); // Wait for 1ms (0.001 seconds)

            // Check if there are any available bytes to read
            if (stream.DataAvailable)
            {
                try
                {
                    Debug.Log("AQUI");
                    int bytesRead = stream.Read(receiveBuffer, 0, receiveBuffer.Length);
                    string response = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                    QueueMultithread.Enqueue(response);
                    Debug.Log("Received: " + response);                    
                }
                catch (Exception e)
                {
                    Debug.LogError("Error receiving message from server: " + e.ToString());
                }
            }

            //if(receivedDirection!=0) 
            //{
            //    Debug.Log("move"+ receivedPlayer+receivedDirection);
            ////move(receivedPlayer, receivedDirection);
            //
            //}

        }
    }

    private void processServerData(string response) {
        if (response.Length > 2)
        {
            char delimiter = '+';
            string[] moveDataString = response.Split(delimiter);
            int player=0;
            int horizontalAxis=0;
            try{
            player= System.Convert.ToInt32(moveDataString[0]);
            }catch(Exception e){                        
                Debug.LogError("parse int "+moveDataString[0]);
            }
            try{
                horizontalAxis= System.Convert.ToInt32(moveDataString[1]);
            }catch(Exception e){
                if(moveDataString.Length>0) Debug.LogError("parse float "+moveDataString[1]);
            }
            
            receivedPlayer = player;
            receivedDirection = horizontalAxis;

            // Store the movement data for the player
            playerMovements[player] = horizontalAxis;

            Debug.Log("RECEIVED PLAYER DIRECTION");
            Debug.Log(receivedPlayer);
            Debug.Log(receivedDirection);
               
        }else{
            playerNum = System.Convert.ToInt32(response);
        }
    }

        string oldPosDoNotFlood; //avoid flooding server, because all players are with send script
    public void Send(string pos){
        if(oldPosDoNotFlood!=pos){
            string message = playerNum + "+" +pos+ "+\n";
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
            oldPosDoNotFlood=pos;      
        }
    }


    private Vector3 movementVector = Vector3.zero;
    private Transform t;
    private Vector3 newPosition = Vector3.zero;
    private void move(int player, int horizontalAxis){
        Debug.Log("index");
        Debug.Log(player);
        Debug.Log(horizontalAxis);
        t=playersTransform[player];

        movementVector.x = horizontalAxis * moveSpeed * Time.deltaTime;
        newPosition = t.position + movementVector;

        //Vector3 newPosition = t.position + new Vector3(horizontalAxis * moveSpeed, 0, 0) * Time.deltaTime;
        t.position = newPosition;        
    }

    // Update is called once per frame
    void FixedUpdate() //rely on physics simulator
    {
        if (MultiplayerController.QueueMultithread.TryDequeue(out string s))
        {
            Debug.Log(s);
            processServerData(s);
        }

        // Process movement for all players
        foreach (var kvp in playerMovements)
        {
            int player = kvp.Key;
            int horizontalAxis = kvp.Value;

            // Apply movement for the player            
            move(player, horizontalAxis);
        }
        //playerMovements.Clear();
        //if (receivedDirection!=0) move(receivedPlayer, receivedDirection);        
    }



    private void OnDestroy()
    {
        // Close the socket connection when the script is destroyed
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
    }
}
