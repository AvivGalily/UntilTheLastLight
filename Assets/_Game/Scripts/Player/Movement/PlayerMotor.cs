using UnityEngine;
namespace Game
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        public CharacterController controller;
        public Camera view;
        public Transform hand;
        public Transform handLight;
        public float Stamina { get; private set; } = 1;
        public bool Moving { get; private set; }
        public bool Sprinting { get; private set; }
        public bool Crouching { get; private set; }
        public float ActualSpeed {get;private set;}
        public float TravelledThisStep {get;private set;}
        public int FootfallsThisStep {get;private set;}
        public float GaitPhase=>Mathf.Repeat(stepProgress*.5f,1);
        public bool Grounded=>controller.enabled&&controller.isGrounded;
        public float StepLength=>Crouching?1.25f:Sprinting?1.45f:1.1f;
        public float sensitivity = .085f, fieldOfView = 80;
        public bool invertY, headBob;
        float pitch, verticalSpeed, recovery, bobTime;
        float stepProgress=.5f;
        public void Simulate(PlayerTuning tuning, Vector2 move, Vector2 look, bool sprint, bool crouch, float dt)
        {
            if (dt <= 0) return;
            transform.Rotate(0, look.x * sensitivity, 0);
            pitch = Mathf.Clamp(pitch + look.y * sensitivity * (invertY ? 1 : -1), -85, 85);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Crouching = crouch;
            if (!crouch && Physics.CheckSphere(transform.position + Vector3.up * 1.65f, .27f, LayerMask.GetMask("WorldSolid", "LightBlocker"), QueryTriggerInteraction.Ignore)) Crouching = true;
            controller.height = Crouching ? 1.35f : 1.9f;
            controller.center = Vector3.up * controller.height / 2;
            Moving = move.sqrMagnitude > .01f;
            Sprinting = sprint && Moving && !Crouching && Stamina > .03f;
            float speed = Crouching ? tuning.crouchSpeed : Sprinting ? tuning.sprintSpeed : tuning.walkSpeed;
            if (Sprinting) { Stamina = Mathf.Max(0, Stamina - dt / tuning.sprintSeconds); recovery = tuning.recoveryDelay; }
            else { recovery -= dt; if (recovery <= 0) Stamina = Mathf.Min(1, Stamina + dt / tuning.refillSeconds); }
            Vector3 direction = transform.right * move.x + transform.forward * move.y;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed -= tuning.gravity * dt;
            Vector3 before=transform.position;
            controller.Move((Vector3.ClampMagnitude(direction, 1) * speed + Vector3.up * verticalSpeed) * dt);
            Vector3 travelled=transform.position-before;travelled.y=0;TravelledThisStep=travelled.magnitude;ActualSpeed=TravelledThisStep/dt;
            Moving=move.sqrMagnitude>.01f&&ActualSpeed>.08f;Sprinting=Sprinting&&Moving;
            FootfallsThisStep=0;
            if(Moving&&Grounded)
            {
                int beforeStep=Mathf.FloorToInt(stepProgress);
                stepProgress+=TravelledThisStep/StepLength;
                FootfallsThisStep=Mathf.FloorToInt(stepProgress)-beforeStep;
            }
            bobTime += dt * (Sprinting ? 11 : 7);
            float bob = headBob && Moving ? Mathf.Sin(bobTime) * .025f : 0;
            view.transform.localPosition = Vector3.up * (Crouching ? tuning.crouchHeight : tuning.standingHeight) + Vector3.up * bob;
            view.fieldOfView = fieldOfView;
            if (hand) hand.localEulerAngles = new Vector3(Mathf.Sin(bobTime) * (Moving ? 1 : .15f), 0, 0);
        }
        public void Teleport(Vector3 position, float yaw)
        {
            controller.enabled = false; transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            controller.enabled = true; pitch = verticalSpeed = 0; Stamina = 1;
            ActualSpeed=TravelledThisStep=0;Moving=Sprinting=Crouching=false;
            stepProgress=.5f;FootfallsThisStep=0;
            view.transform.localRotation = Quaternion.identity;
        }
    }

}
