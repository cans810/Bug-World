using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIMessageQueue : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeTime = 0.5f;
    [SerializeField] private int maxQueueSize = 10;
    
    private Queue<string> messageQueue = new Queue<string>();
    private bool isDisplayingMessage = false;
    private Coroutine displayCoroutine;
    
    // Call this method to queue a new message
    public void EnqueueMessage(string message)
    {
        // Don't add duplicate messages in a row
        if (messageQueue.Count > 0 && messageQueue.Peek() == message)
            return;
            
        // Limit queue size
        if (messageQueue.Count >= maxQueueSize)
        {
            Debug.LogWarning("Message queue is full, dropping oldest message");
            messageQueue.Dequeue();
        }
        
        // Add the new message to the queue
        messageQueue.Enqueue(message);
        
        // Start displaying messages if not already
        if (!isDisplayingMessage)
        {
            displayCoroutine = StartCoroutine(DisplayMessages());
        }
    }
    
    private IEnumerator DisplayMessages()
    {
        isDisplayingMessage = true;
        
        while (messageQueue.Count > 0)
        {
            // Get the next message
            string message = messageQueue.Dequeue();
            
            // Set the text
            if (messageText != null)
            {
                messageText.gameObject.SetActive(true);
                messageText.text = message;
                messageText.alpha = 1f;
                
                // Wait for the display duration
                yield return new WaitForSeconds(displayDuration);
                
                // Fade out
                float time = 0;
                while (time < fadeTime)
                {
                    time += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, time / fadeTime);
                    messageText.alpha = alpha;
                    yield return null;
                }
                
                messageText.alpha = 0f;
                messageText.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(displayDuration);
            }
        }
        
        isDisplayingMessage = false;
    }
    
    // Add a method to clear the queue if needed
    public void ClearQueue()
    {
        messageQueue.Clear();
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }
        
        if (messageText != null)
        {
            messageText.alpha = 0f;
            messageText.gameObject.SetActive(false);
        }
        
        isDisplayingMessage = false;
    }
} 