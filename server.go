package main

import (
	"bufio"
	"fmt"
	"net"
	"strings"
	"sync"
	"strconv"
)

func handleClient(conn net.Conn, clients *map[int]net.Conn, mutex *sync.Mutex) {
	defer conn.Close()

	fmt.Println("Client connected:", conn.RemoteAddr())

	// Add the client connection to the map with an assigned index
	mutex.Lock()
	index := len(*clients)
	(*clients)[index] = conn
	mutex.Unlock()

	// Create a reader to read messages from the client
	reader := bufio.NewReader(conn)

	for {
		// Read a message from the client
		message, err := reader.ReadString('\n')
		if err != nil {
			fmt.Println("Client disconnected:", conn.RemoteAddr())

			// Remove the client connection from the map
			mutex.Lock()
			delete(*clients, index)
			mutex.Unlock()

			return
		}

		// Print the received message
		fmt.Println("Received from client", index, ":", message)

		// Modify and send a response back to the client
		delimiter := "+"
		messageParts := strings.Split(message, delimiter)
		//messageParts[0] = strconv.Itoa(index)
		messageParts[0] = strconv.Itoa(index)
		fmt.Println("Broadcast from client " + strconv.Itoa(index))
		response := strings.Join(messageParts, "+") + "\n"
		conn.Write([]byte(response))
		message = response

		// Update all client connections with the message (broadcast)
		mutex.Lock()
		for i, c := range *clients {
			if i != index {
				// c.Write([]byte("Broadcast: " + message))
				c.Write([]byte(message))
				fmt.Println("Broadcast from client " + strconv.Itoa(index) + ": " + message)
			}
		}
		mutex.Unlock()
	}
}

func main() {
	clients := make(map[int]net.Conn)
	mutex := &sync.Mutex{}

	// Listen for incoming connections on port 8080
	listener, err := net.Listen("tcp", ":8080")
	if err != nil {
		fmt.Println("Error:", err)
		return
	}
	defer listener.Close()

	fmt.Println("Server listening on :8080")

	for {
		// Accept incoming connections
		conn, err := listener.Accept()
		if err != nil {
			fmt.Println("Error accepting connection:", err)
			continue
		}

		// Handle the client in a separate goroutine
		go handleClient(conn, &clients, mutex)
	}
}
