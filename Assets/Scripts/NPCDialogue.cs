using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "Dialogue/NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
    [Header("NPC Info")]
    public string npcName;
    public Sprite npcPortrait;
    
    [Header("Dialogue Content")]
    [TextArea(2, 5)] // Rende più comodo scrivere dialoghi lunghi
    public string[] dialogueLines;
    
    [Header("Timing Settings")]
    [Range(0.01f, 0.1f)]
    public float typingSpeed = 0.05f;
    public float autoProgressDelay = 1.5f;
    public bool[] autoProgressLines; // Una per ogni linea
    
    [Header("Audio Settings")]
    public AudioClip voiceSound;
    [Range(0.5f, 2f)]
    public float voicePitch = 1f;
    
    // Validazione per evitare errori
    private void OnValidate()
    {
        if (autoProgressLines == null || autoProgressLines.Length != dialogueLines.Length)
        {
            autoProgressLines = new bool[dialogueLines.Length];
        }
    }
}