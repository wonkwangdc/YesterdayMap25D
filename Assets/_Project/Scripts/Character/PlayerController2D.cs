using UnityEngine;
using UnityEngine.InputSystem;

namespace YesterdayMap.Character
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float moveSpeed = 4f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 2f;
        [SerializeField] private Rigidbody2D body;
        private Vector2 moveInput;

        private void Awake()
        {
            if (!TryGetComponent<CharacterFootstepAudio>(out _))
                gameObject.AddComponent<CharacterFootstepAudio>();

            if (body == null)
                body = GetComponent<Rigidbody2D>();
        }

        public void Configure(Rigidbody2D rigidbody2D)
        {
            body = rigidbody2D;
        }

        private void Update()
        {
            if (Keyboard.current == null) { moveInput = Vector2.zero; return; }
            float x = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            float y = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
            moveInput = new Vector2(x, y).normalized;
        }

        private void FixedUpdate()
        {
            bool isSprinting = Keyboard.current != null &&
                               (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            float currentSprintMultiplier = isSprinting ? sprintMultiplier : 1f;
            body.MovePosition(body.position + moveInput * (moveSpeed * currentSprintMultiplier * Time.fixedDeltaTime));
        }
    }
}
