using UnityEngine;

namespace KartGame.KartSystems {

    public class KeyboardInput : BaseInput
    {
        public string TurnInputName = "Horizontal";
        public string AccelerateButtonName = "Accelerate";
        public string BrakeButtonName = "Brake";
        float xVal;
        bool goButton;
        bool stopButton;

        public override InputData GenerateInput() {
            return new InputData
            {
                Accelerate = goButton,
                Brake = stopButton,
                TurnInput = xVal
            };
        }

        private void Update()
        {
            if (this.isLocalPlayer)
            {
                xVal = Input.GetAxis("Horizontal");
                goButton = Input.GetButton(AccelerateButtonName);
                stopButton = Input.GetButton(BrakeButtonName);
            }            
        }
    }
}
