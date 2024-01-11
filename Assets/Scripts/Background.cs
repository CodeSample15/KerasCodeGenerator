using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    [SerializeField] private GameObject dotSprite;
    [SerializeField] public float dotDistance;

    private List<GameObject> dots;
    private int maxDotsNeeded; //for the garbage collector to know how many dots needed to be destroyed

    void Awake()
    {
        dots = new List<GameObject>();

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        screenSize = Camera.main.ScreenToWorldPoint(screenSize);

        for(float y=-screenSize.y-dotDistance; y<=screenSize.y+dotDistance; y+=dotDistance) {
            for(float x=-screenSize.x-dotDistance; x<=screenSize.x+dotDistance; x+=dotDistance) {
                GameObject temp = Instantiate(dotSprite, new Vector2(x, y), Quaternion.identity);
                temp.transform.parent = transform;
                dots.Add(temp);
            }
        }

        StartCoroutine(UpdateDots());
        StartCoroutine(DotGarbageCollector());
    }

    IEnumerator UpdateDots() {
        while(true) {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            screenSize = Camera.main.ScreenToWorldPoint(screenSize);

            //calculate offset
            float offX = transform.position.x % dotDistance;
            float offY = transform.position.y % dotDistance;

            int i = 0;
            for(float y=-screenSize.y-dotDistance; y<=screenSize.y+dotDistance; y+=dotDistance) {
                for(float x=-screenSize.x-dotDistance; x<=screenSize.x+dotDistance; x+=dotDistance) {
                    if(i<dots.Count) {
                        dots[i].transform.position = new Vector2(x+offX, y+offY);
                        dots[i].SetActive(true);
                    }
                    else {
                        GameObject temp = Instantiate(dotSprite, Vector2.zero, Quaternion.identity);
                        temp.transform.parent = transform;
                        temp.transform.position = new Vector2(x, y);
                        dots.Add(temp);
                    }

                    i++;
                }
            }

            maxDotsNeeded = i;

            //hide all the extras
            for(; i<dots.Count; i++) {
                if(!dots[i].activeSelf)
                    break; //stop once you reach non activated dots

                dots[i].SetActive(false);
            }

            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator DotGarbageCollector() {
        while(true) {
            if(dots.Count > maxDotsNeeded * 1.5f) {
                for(int i=(int)(maxDotsNeeded*1.3f); i<dots.Count;) {
                    Destroy(dots[i]);
                    dots.RemoveAt(i);
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }
}
