using System;
using System.Net.Sockets;
using System.Text;
using System.Collections;
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

    // Start is called before the first frame update
    private void Start()
    {
        try
        {
            //client = new TcpClient("game.055190.xyz", 8080); // Connect to the Go server on localhost and port 8080
            client = new TcpClient("10.126.36.29", 8080); // Connect to the Go server 
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
                    Debug.Log("Received: " + response);
                    
                    if(response.Length>1){
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

                        Debug.Log("RECEIVED PLAYER DIRECTION");
                        Debug.Log(receivedPlayer);
                        Debug.Log(receivedDirection);
                       
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error receiving message from server: " + e.ToString());
                }
            }

            move(receivedPlayer, receivedDirection);
        }
    }

    public void Send(string pos){        
        string message = playerNum + "+" +pos+ "+\n";
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }


    Transform t;
    private void move(int player, int horizontalAxis){
        //Debug.Log("index");
        //Debug.Log(player);
        //Debug.Log(horizontalAxis);
        t=playersTransform[player];
        Vector3 newPosition = t.position + new Vector3(horizontalAxis * moveSpeed, 0, 0) * Time.deltaTime;
        t.position = newPosition;        
    }

    // Update is called once per frame
    //void Update()
    //{
    //    if(receivedDirection!=0) move(receivedPlayer, receivedDirection);        
    //}

    

    private void OnDestroy()
    {
        // Close the socket connection when the script is destroyed
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
    }
}