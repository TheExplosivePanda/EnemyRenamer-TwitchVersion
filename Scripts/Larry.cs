using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

namespace EnemyRenamerTwitch
{
    class Larry : MonoBehaviour
    {
        dfLabel nameLabel = null;//enemy name, appears under the sprite in the middle
        dfLabel SpeechBubble = null;//label used to make chat messeges pop in game
        public static Dictionary<string, List<Larry>> EnemyDictionary = new Dictionary<string, List<Larry>>();//hash of all names to improve preformance, also used to push new messeges into queue via list
        public static List<string> namesDB = new List<string>();
        tk2dSprite sprite = null;//if sprite exists, its used to center the label under the sprite
        Queue<string> messegeQueue = new Queue<string>();//quque of messeges to be "popped"
        float nameXoffset = 0;//offset used to center name label
        IEnumerator coroutine = null;//MessegePop corutine
        bool isCoroutineRunning = false;
        public static float nameSize = 3;
        public static float msgSize = 3;
        public static int msgLength = 25;
        /// <summary>
        /// instnatiates and attaches a name label to the base game object, sets the "sprite" variable, and calculates x offset
        /// gets a random name and color to give the name
        /// adds this monobehavior to the list inside the dictionary to allow messeges to be popped
        /// also instantiates the speech bubble variable and gives it the same color as the nameLabel
        /// </summary>
        void Start()
        {
            string Name = "NamerMod:NoNamesFound";
            if (namesDB != null && namesDB.Count>0)
            {
                Name = namesDB[UnityEngine.Random.Range(0,namesDB.Count)];
                if (EnemyDictionary.ContainsKey(Name))
                {
                    EnemyDictionary[Name].Add(this);
                }               
            }
            
            
            sprite = this.gameObject.GetComponent<tk2dSprite>();
            GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(BraveResources.Load("DamagePopupLabel", ".prefab"), GameUIRoot.Instance.transform);

            Vector3 worldPosition = this.transform.position;

            nameLabel = gameObject.GetComponent<dfLabel>();
            nameLabel.gameObject.SetActive(true);
            nameLabel.Text = Name;
            nameLabel.Color = Color.HSVToRGB(UnityEngine.Random.value, 1.0f, 1.0f);
            nameLabel.Opacity = 0.95f;
            nameLabel.TextScale = nameSize;
            nameLabel.transform.position = dfFollowObject.ConvertWorldSpaces(worldPosition, GameManager.Instance.MainCameraController.Camera, GameUIRoot.Instance.Manager.RenderCamera).WithZ(0f);
            nameLabel.transform.position = nameLabel.transform.position.QuantizeFloor(nameLabel.PixelsToUnits() / (Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale));
           
            if (sprite != null)
            {
                GameObject SpeechObj = (GameObject)UnityEngine.Object.Instantiate(BraveResources.Load("DamagePopupLabel", ".prefab"), GameUIRoot.Instance.transform);
                SpeechBubble = SpeechObj.GetComponent<dfLabel>();
                SpeechBubble.Color = nameLabel.Color;
                SpeechBubble.Text = "";
                SpeechBubble.Opacity = 0.9f;
                SpeechBubble.TextScale = msgSize;
            }
            
            nameXoffset = nameLabel.GetCenter().x - nameLabel.transform.position.x;
        }
        /// <summary>
        /// updates the position of the nameLabel every frame using the sprite bottom center  and offset for centering, if no sprite exists, uses game objects position
        /// also updates speechbubble position
        /// checks if the messge queue has any messeges in it running and if the MessegePop coroutine is running.
        /// if there are messeges pending and the coroutine isnt running, pops a new messege
        /// </summary>
        void Update()
        {
            Vector3 worldPosition = this.transform.position;
            if (sprite != null)
            {
                worldPosition = sprite.WorldBottomCenter;
                if (SpeechBubble != null)
                {
                    SpeechBubble.transform.position = dfFollowObject.ConvertWorldSpaces(sprite.WorldTopLeft, GameManager.Instance.MainCameraController.Camera, GameUIRoot.Instance.Manager.RenderCamera).WithZ(0f);
                    SpeechBubble.transform.position = SpeechBubble.transform.position.WithX(SpeechBubble.transform.position.x - (SpeechBubble.GetCenter().x -SpeechBubble.transform.position.x)  );
                    SpeechBubble.transform.position = SpeechBubble.transform.position.WithY(SpeechBubble.transform.position.y + 0.0625f*2f);
                    SpeechBubble.transform.position = SpeechBubble.transform.position.QuantizeFloor(SpeechBubble.PixelsToUnits() / (Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale));
                    SpeechBubble.IsVisible = !GameManager.Instance.IsPaused;
                }               
            }
            if (messegeQueue.Count != 0 && !isCoroutineRunning)
            {
                HandleSpeakInternal(messegeQueue.Dequeue());
            }
            if (nameLabel)
            {
                Vector2 tempPos = dfFollowObject.ConvertWorldSpaces(worldPosition, GameManager.Instance.MainCameraController.Camera, GameUIRoot.Instance.Manager.RenderCamera).WithZ(0f);
                nameLabel.transform.position = tempPos.WithX(tempPos.x - nameXoffset);
                nameLabel.transform.position = nameLabel.transform.position.QuantizeFloor(nameLabel.PixelsToUnits() / (Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale));
                nameLabel.IsVisible = !GameManager.Instance.IsPaused;
            }
        }
        /// <summary>
        /// stops all corotines, removes this game object from the list of game objects that can have a messege pusehd on to them, abd destroyes both name and speech labels
        /// </summary>
        void OnDestroy()
        {
            StopAllCoroutines();
            if (EnemyDictionary.ContainsKey(nameLabel.Text))
            {
                EnemyDictionary[nameLabel.Text].Remove(this);
            }           
            UnityEngine.Object.Destroy(nameLabel.gameObject);
            UnityEngine.Object.Destroy(SpeechBubble.gameObject);

        }
        /// <summary>
        /// used by external classes to "push" messeges to be "popped". in this case messeges from chat will pop from their respective owners.
        /// </summary>
        /// <param name="text">text to be "popped"</param>
        public void PushMessegeToMessegeQueue(string text)
        {
            messegeQueue.Enqueue(text);
        }
        /// <summary>
        /// used by the update method to pop a messege. makes sure the messege isnt longer than 10 characters, and if so shortens it, adding "..." to the end
        /// starts the MessegePop coroutine
        /// </summary>
        /// <param name="text">text to be "popped"</param>
        private void HandleSpeakInternal(string text)
        {
            if (text.Length > msgLength)
            {
                text = text.Substring(0,msgLength-1) + "...";
            }
            if (SpeechBubble != null)
            {
                //coroutine = MessegePop(this.gameObject.transform.position,1,text);
                coroutine = MessegeHover(text);
                StartCoroutine(coroutine);
                isCoroutineRunning = true;
            }
        }
        /// <summary>
        /// makes the speech label show some text for 3 seconds
        /// </summary>
        /// <param name="text"> text to show</param>
        /// <returns></returns>
        private IEnumerator MessegeHover(string text)
        {
            isCoroutineRunning = true;
            if (SpeechBubble!= null)
            {
                SpeechBubble.Text = text;
                yield return new WaitForSecondsRealtime(3);
                SpeechBubble.Text = "";
            }
            yield return null;
            isCoroutineRunning = false;
        }
        /// <summary>
        /// UNUSED coroutine takes from base game code, takes text and makes it fly up and fall down similiarly to scouters damage indicators.
        /// in this case slightly modified to only use SpeechLabel, and to set isCoroutineRunning to false at the end.
        /// </summary>
        /// <param name="startWorldPosition">where to be popped</param>
        /// <param name="worldFloorHeight">height off floor, works with 1, should probably figure it out properly sometimes</param>
        /// <param name="text">text to be shown</param>
        /// <returns></returns>
        private IEnumerator MessegePop(Vector3 startWorldPosition, float worldFloorHeight, string text)
        {
            SpeechBubble.Text = text;
            float elapsed = 0f;
            float duration = 1.5f;
            float holdTime = 0f;
            Camera mainCam = GameManager.Instance.MainCameraController.Camera;
            Vector3 worldPosition = startWorldPosition;
            Vector3 lastVelocity = new Vector3(Mathf.Lerp(-8f, 8f, UnityEngine.Random.value), UnityEngine.Random.Range(15f, 25f), 0f);
            while (elapsed < duration)
            {
                float dt = BraveTime.DeltaTime;
                elapsed += dt;
                if (GameManager.Instance.IsPaused)
                {
                    break;
                }
                if (elapsed > holdTime)
                {
                    lastVelocity += new Vector3(0f, -50f, 0f) * dt;
                    Vector3 vector = lastVelocity * dt + worldPosition;
                    if (vector.y < worldFloorHeight)
                    {
                        float num = worldFloorHeight - vector.y;
                        float num2 = worldFloorHeight + num;
                        vector.y = num2 * 0.5f;
                        lastVelocity.y *= -0.5f;
                    }
                    worldPosition = vector;
                    SpeechBubble.transform.position = dfFollowObject.ConvertWorldSpaces(worldPosition, mainCam, GameUIRoot.Instance.Manager.RenderCamera).WithZ(0f);
                }
                float t = elapsed / duration;
                SpeechBubble.Opacity = 1f - t;
                yield return null;
            }
            isCoroutineRunning = false;
            SpeechBubble.Text = "";
            yield break;
        }
    }
}
