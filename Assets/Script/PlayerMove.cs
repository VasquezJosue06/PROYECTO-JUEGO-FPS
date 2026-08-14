using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public float movespeed = 5f;
    public float jumpforce = 5f;

    public Transform groundchek;
    public float grounddistance = 0.4f;
    public LayerMask groundMask;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isgrounded;
    private PlayerInput playerInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = new PlayerInput();
    }


    void Update()
    {
        
    }

    void OnJump()
    {
        rb.AddForce(new Vector3(0, jumpforce, 0), ForceMode.Impulse);
    }
}
