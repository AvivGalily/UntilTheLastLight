using UnityEngine;
namespace Game
{
    public sealed class IntroTransitionBridge : MonoBehaviour
    {
        public GameObject environment;
        public Transform cabinAnchor;
        public Camera mirrorCamera;
        GameSession game;
        Transform parent;
        Vector3 localPosition;
        Quaternion localRotation;
        float fov,fog;
        bool hand,controller,torch,attached;
        public void Attach(GameSession session,Transform driver)
        {
            game=session;var eyes=game.player.view.transform;
            parent=eyes.parent;localPosition=eyes.localPosition;localRotation=eyes.localRotation;
            fov=game.player.view.fieldOfView;fog=RenderSettings.fogDensity;
            hand=game.player.hand.gameObject.activeSelf;controller=game.player.controller.enabled;torch=game.flashlightVisual.enabled;
            game.player.controller.enabled=false;game.player.hand.gameObject.SetActive(false);game.flashlightVisual.enabled=false;
            game.player.gameObject.SetActive(true);environment.SetActive(true);
            eyes.SetParent(driver,false);eyes.localPosition=Vector3.zero;eyes.localRotation=Quaternion.identity;
            game.player.view.fieldOfView=76;if(mirrorCamera)mirrorCamera.enabled=true;attached=true;
        }
        public void Cabin(float progress)
        {
            if(!attached)return;environment.SetActive(false);if(mirrorCamera)mirrorCamera.enabled=false;
            game.player.transform.SetPositionAndRotation(cabinAnchor.position,cabinAnchor.rotation);
            var eyes=game.player.view.transform;eyes.SetParent(parent,false);
            float rise=Mathf.SmoothStep(0,1,progress);
            eyes.localPosition=Vector3.Lerp(new Vector3(0,.48f,-.1f),localPosition,rise);
            eyes.localRotation=Quaternion.Slerp(Quaternion.Euler(-32,0,-8),localRotation,rise);
            game.player.view.fieldOfView=Mathf.Lerp(72,fov,rise);RenderSettings.fogDensity=fog;
        }
        public void Restore()
        {
            if(!attached)return;
            var eyes=game.player.view.transform;eyes.SetParent(parent,false);eyes.localPosition=localPosition;eyes.localRotation=localRotation;
            game.player.view.fieldOfView=fov;game.player.controller.enabled=controller;game.player.hand.gameObject.SetActive(hand);game.flashlightVisual.enabled=torch;
            environment.SetActive(false);if(mirrorCamera)mirrorCamera.enabled=false;RenderSettings.fogDensity=fog;attached=false;
        }
    }
}
