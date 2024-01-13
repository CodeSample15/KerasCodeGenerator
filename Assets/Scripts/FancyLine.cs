using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class FancyLine : MonoBehaviour
{
    [SerializeField] private Color32 NormalColor;
    [SerializeField] private Color32 SelectedColor;
    [SerializeField] private float ColorFadeTime;

    [Space]

    [SerializeField] private float NormalWidth;
    [SerializeField] private float HighlightedWidth;
    [SerializeField] private float WidthChangeTime;

    [Space]

    public GameObject StartPos;
    public GameObject EndPos;

    public bool selected;
    public static FancyLine selectedLine;
    public static FancyLine highlightedLine;

    private LineRenderer line;
    private PolygonCollider2D lineCollider;

    //cosmetic stuff
    private float colorTransitionAmount;
    private float colorTransitionVelocity;

    private float widthChangeVelocity;

    void Awake() 
    {
        line = GetComponent<LineRenderer>();
        lineCollider = GetComponent<PolygonCollider2D>();
        line.positionCount = 2;
        selected = false;

        line.startColor = NormalColor;
        line.endColor = NormalColor;

        line.startWidth = NormalWidth;
        line.endWidth = NormalWidth;

        colorTransitionAmount = 0;
        colorTransitionVelocity = 0;
    }

    void Update() 
    {
        if(StartPos == null || EndPos == null)
            Destroy(gameObject);
        else {
            //TODO: make lines fancy and curvy so they live up to their name of "FancyLine"
            //currently only drawing a straight line so I can get through this project quicker
            line.SetPosition(0, (Vector2)StartPos.transform.position);
            line.SetPosition(1, (Vector2)EndPos.transform.position);

            //transition colors
            line.startColor = Color32.Lerp(NormalColor, SelectedColor, colorTransitionAmount);
            line.endColor = Color32.Lerp(NormalColor, SelectedColor, colorTransitionAmount);

            colorTransitionAmount = Mathf.SmoothDamp(colorTransitionAmount, selected ? 1 : 0, ref colorTransitionVelocity, ColorFadeTime);

            selected = selectedLine == this;

            //reactivity when mouse is hovered over the line
            float lineWidth = Mathf.SmoothDamp(line.startWidth, highlightedLine == this ? HighlightedWidth : NormalWidth, ref widthChangeVelocity, WidthChangeTime);
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;

            //calculate polygon collider for mouse clicks
            Vector2[] colliderPoints = new Vector2[line.positionCount*2];
            for(int i=0; i<line.positionCount-1; i++) {
                //helper variables to keep the code relatively clean
                Vector2 point1 = line.GetPosition(i);
                Vector2 point2 = line.GetPosition(i+1);

                //calculate slope and line width
                Vector2 delta1;
                Vector2 delta2;
                float l_width = line.startWidth * 2.3f;

                if(point1.x!=point2.x) {
                    float l_m = (point1.y - point2.y) / (point1.x - point2.x);

                    float dx;
                    float dy;

                    dx = (l_width/2) * (l_m / Mathf.Sqrt(l_m*l_m + 1));
                    dy = (l_width/2) * (1 / Mathf.Sqrt(l_m*l_m + 1));
                    delta1 = new Vector2(-dx, dy);
                    delta2 = new Vector2(dx, -dy);
                }
                else {
                    delta1 = new Vector2(l_width/2, 0);
                    delta2 = new Vector2(-l_width/2, 0);
                }

                colliderPoints[i] = point1 + delta1;
                colliderPoints[i+1] = point2 + delta1;
                colliderPoints[i+2] = point2 + delta2;
                colliderPoints[i+3] = point1 + delta2;
            }

            lineCollider.SetPath(0, colliderPoints);
        }
    }

    public void Delete()
    {
        GraphNode startGraph = StartPos.GetComponentInParent<GraphNode>();
        GraphNode endGraph = EndPos.GetComponentInParent<GraphNode>();

        //hoo boy, try to ignore how stupid this is
        //just check all of the lists if the object is in it and delete
        startGraph.removeConnection(endGraph);
        endGraph.removeConnection(startGraph);
        
        Destroy(gameObject);
    }
}
