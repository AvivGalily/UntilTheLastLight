using System;
namespace Game
{
    [Serializable] public sealed class PlayerTuning
    {
        public float walkSpeed = 3, sprintSpeed = 5.2f, crouchSpeed = 1.6f;
        public float sprintSeconds = 8, recoveryDelay = 1.5f, refillSeconds = 5;
        public float standingHeight = 1.7f, crouchHeight = 1.15f, interactionRange = 2.2f;
        public float hitInvulnerability = 6, woundRecovery = 15, gravity = 20;
    }
}
