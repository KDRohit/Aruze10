using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[AddComponentMenu("Telos/NGUI/UI/Line Sprite")]
public class TIUILineSprite : UISprite
{
    #region Enumerations
    public enum CornerTypes
    {
        Point,
        Chamfered,
    }
    #endregion Enumerations

    #region Members
    [SerializeField]
    private float m_width;
    [SerializeField]
    private float m_aaWidth;
    [SerializeField]
    private Vector2[] m_points;
    [SerializeField]
    private CornerTypes m_cornerType;
    private Pivot m_previousPivot;
    private Vector2[] m_vertices;
    #endregion Members

    #region Properties
    public float Width
    {
        get { return this.m_width; }
        set
        {
            this.m_width = value;
            this.UpdateUVs(true);
        }
    }
    public float AaWidth
    {
        get { return this.m_aaWidth; }
        set
        {
            this.m_aaWidth = Mathf.Clamp(value, 0f, this.Width / 2);
            this.UpdateUVs(true);
        }
    }
    public Vector2[] Points
    {
        get { return this.m_points; }
        set
        {
            this.m_points = value;
            this.UpdateUVs(true);
        }
    }
    public CornerTypes CornerType
    {
        get { return this.m_cornerType; }
        set
        {
            this.m_cornerType = value;
            this.UpdateUVs(true);
        }
    }
    private Pivot PreviousPivot
    {
        get { return this.m_previousPivot; }
        set { this.m_previousPivot = value; }
    }
    private Vector2[] Vertices
    {
        get { return this.m_vertices; }
        set { this.m_vertices = value; }
    }
    #endregion Properties

    #region UISPrite Overrides
    override public void MakePixelPerfect()
    {
        this.cachedTransform.localScale = new Vector3(1, 1, 1);
    }

	public override void Update ()
	{
		if (this.pivot != this.PreviousPivot)
		{
			this.UpdateUVs(true);
			this.PreviousPivot = this.pivot;
		}
		base.Update();
	}

    override public void UpdateUVs(bool force)
    {
        if (force)
        {
            base.UpdateUVs(force);

            //First segment
            if (this.Points != null && this.Points.Length < 2)
            {
                this.Vertices = null;
                return;
            }

            //Calculate line edges
            Rect bounds = new Rect();
            Vector2 cur, curPerp, aaBot, bot, top, aaTop;
            Vector2 prev, prevPerp;
            float compensation, aaCompensation, angle;
            List<LineEdge> edges = new List<LineEdge>((this.Points.Length - 1) * 2);

            //First Edge
            cur = (this.Points[1] - this.Points[0]).normalized;
            curPerp = new Vector2(cur.y, -cur.x).normalized / 2;
            aaBot = this.Points[0] + curPerp * this.Width;
            aaTop = this.Points[0] - curPerp * this.Width;
            top = this.Points[0] - curPerp * (this.Width - 2 * this.AaWidth);
            bot = this.Points[0] + curPerp * (this.Width - 2 * this.AaWidth);
            edges.Add(new LineEdge(aaBot, bot, top, aaTop));
            bounds.xMin = Mathf.Min(bounds.xMin, aaTop.x, top.x, bot.x, aaBot.x);
            bounds.xMax = Mathf.Max(bounds.xMax, aaTop.x, top.x, bot.x, aaBot.x);
            bounds.yMin = Mathf.Min(bounds.yMin, aaTop.y, top.y, bot.y, aaBot.y);
            bounds.yMax = Mathf.Max(bounds.yMax, aaTop.y, top.y, bot.y, aaBot.y);
            

            //Subsequent Edges
            switch (this.CornerType)
            {
                case CornerTypes.Chamfered:
                    //Second Edge
                    aaBot = this.Points[1] + curPerp * this.Width;
                    aaTop = this.Points[1] - curPerp * this.Width;
                    top = this.Points[1] - curPerp * (this.Width - 2 * this.AaWidth);
                    bot = this.Points[1] + curPerp * (this.Width - 2 * this.AaWidth);
                    edges.Add(new LineEdge(aaBot, bot, top, aaTop));
                    bounds.xMin = Mathf.Min(bounds.xMin, aaTop.x, top.x, bot.x, aaBot.x);
                    bounds.xMax = Mathf.Max(bounds.xMax, aaTop.x, top.x, bot.x, aaBot.x);
                    bounds.yMin = Mathf.Min(bounds.yMin, aaTop.y, top.y, bot.y, aaBot.y);
                    bounds.yMax = Mathf.Max(bounds.yMax, aaTop.y, top.y, bot.y, aaBot.y);

                    for (int i = 2; i < this.Points.Length; i++)
                    {
                        cur = (this.Points[i] - this.Points[i - 1]).normalized;
                        curPerp = new Vector2(cur.y, -cur.x).normalized / 2;
                        aaBot = this.Points[i - 1] + curPerp * this.Width;
                        aaTop = this.Points[i - 1] - curPerp * this.Width;
                        top = this.Points[i - 1] - curPerp * (this.Width - 2 * this.AaWidth);
                        bot = this.Points[i - 1] + curPerp * (this.Width - 2 * this.AaWidth);
                        edges.Add(new LineEdge(aaBot, bot, top, aaTop));
                        bounds.xMin = Mathf.Min(bounds.xMin, aaTop.x, top.x, bot.x, aaBot.x);
                        bounds.xMax = Mathf.Max(bounds.xMax, aaTop.x, top.x, bot.x, aaBot.x);
                        bounds.yMin = Mathf.Min(bounds.yMin, aaTop.y, top.y, bot.y, aaBot.y);
                        bounds.yMax = Mathf.Max(bounds.yMax, aaTop.y, top.y, bot.y, aaBot.y);

                        aaBot = this.Points[i] + curPerp * this.Width;
                        aaTop = this.Points[i] - curPerp * this.Width;
                        top = this.Points[i] - curPerp * (this.Width - 2 * this.AaWidth);
                        bot = this.Points[i] + curPerp * (this.Width - 2 * this.AaWidth);
                        edges.Add(new LineEdge(aaBot, bot, top, aaTop));
                        bounds.xMin = Mathf.Min(bounds.xMin, aaTop.x, top.x, bot.x, aaBot.x);
                        bounds.xMax = Mathf.Max(bounds.xMax, aaTop.x, top.x, bot.x, aaBot.x);
                        bounds.yMin = Mathf.Min(bounds.yMin, aaTop.y, top.y, bot.y, aaBot.y);
                        bounds.yMax = Mathf.Max(bounds.yMax, aaTop.y, top.y, bot.y, aaBot.y);
                    }
                    break;
                default:
                    for (int i = 2; i < this.Points.Length; i++)
                    {
                        prev = cur;
                        prevPerp = curPerp;
                        cur = (this.Points[i] - this.Points[i - 1]).normalized;
                        curPerp = new Vector2(cur.y, -cur.x).normalized / 2;
                        angle = AngleSigned(prev, cur, new Vector3(0, 0, 1));
                        aaCompensation = Mathf.Abs(Mathf.Tan(Mathf.Deg2Rad * angle/2)) * this.Width / 2;
                        compensation = Mathf.Abs(Mathf.Tan(Mathf.Deg2Rad * angle / 2)) * (this.Width - 2 * this.AaWidth) / 2;

                        if (angle < 0)
                        {
                            aaBot = this.Points[i - 1] + prevPerp * this.Width - prev * aaCompensation;
                            aaTop = this.Points[i - 1] - prevPerp * this.Width + prev * aaCompensation;
                            top = this.Points[i - 1] - prevPerp * (this.Width - 2 * this.AaWidth) + prev * compensation;
                            bot = this.Points[i - 1] + prevPerp * (this.Width - 2 * this.AaWidth) - prev * compensation;
                        }
                        else
                        {
                            aaBot = this.Points[i - 1] + prevPerp * this.Width + prev * aaCompensation;
                            aaTop = this.Points[i - 1] - prevPerp * this.Width - prev * aaCompensation;
                            top = this.Points[i - 1] - prevPerp * (this.Width - 2 * this.AaWidth) - prev * compensation;
                            bot = this.Points[i - 1] + prevPerp * (this.Width - 2 * this.AaWidth) + prev * compensation;
                        }
                        edges.Add(new LineEdge(aaBot, bot, top, aaTop));
                        bounds.xMin = Mathf.Min(bounds.xMin, aaTop.x, top.x, bot.x, aaBot.x);
                        bounds.xMax = Mathf.Max(bounds.xMax, aaTop.x, top.x, bot.x, aaBot.x);
                        bounds.yMin = Mathf.Min(bounds.yMin, aaTop.y, top.y, bot.y, aaBot.y);
                        bounds.yMax = Mathf.Max(bounds.yMax, aaTop.y, top.y, bot.y, aaBot.y);
                    }

                    //Last Edge
                    cur = (this.Points[this.Points.Length - 1] - this.Points[this.Points.Length - 2]).normalized;
                    curPerp = new Vector2(cur.y, -cur.x).normalized / 2;
                    aaBot = this.Points[this.Points.Length - 1] + curPerp * this.Width;
                    aaTop = this.Points[this.Points.Length - 1] - curPerp * this.Width;
                    top = this.Points[this.Points.Length - 1] - curPerp * (this.Width - 2 * this.AaWidth);
                    bot = this.Points[this.Points.Length - 1] + curPerp * (this.Width - 2 * this.AaWidth);
                    edges.Add(new LineEdge(aaBot, bot, top, aaTop));
                    bounds.xMin = Mathf.Min(bounds.xMin, aaTop.x, top.x, bot.x, aaBot.x);
                    bounds.xMax = Mathf.Max(bounds.xMax, aaTop.x, top.x, bot.x, aaBot.x);
                    bounds.yMin = Mathf.Min(bounds.yMin, aaTop.y, top.y, bot.y, aaBot.y);
                    bounds.yMax = Mathf.Max(bounds.yMax, aaTop.y, top.y, bot.y, aaBot.y);
                    break;
            }

            Vector2 boundsCenter = new Vector2(bounds.xMin + bounds.width / 2, bounds.yMin + bounds.height / 2);
            Vector2 anchorOffset = new Vector2();
            if (this.pivot == Pivot.Center)
            {
                anchorOffset = -boundsCenter;
            }
            else
            {
                if (this.pivot == Pivot.Bottom || this.pivot == Pivot.BottomLeft || this.pivot == Pivot.BottomRight)
                {
                    anchorOffset.y = -bounds.yMin;
                }
                if (this.pivot == Pivot.Right || this.pivot == Pivot.BottomRight || this.pivot == Pivot.TopRight)
                {
                    anchorOffset.x = -bounds.xMax;
                }

                if (this.pivot == Pivot.Top || this.pivot == Pivot.Bottom)
                {
                    anchorOffset.x = -boundsCenter.x;
                }
                if (this.pivot == Pivot.Left || this.pivot == Pivot.Right)
                {
                    anchorOffset.y = -boundsCenter.y;
                }
            }

            //Construct Vertices
            List<Vector2> verts = new List<Vector2>();
            for (int i = 1; i < edges.Count; i++)
            {
                verts.Add(edges[i].AaTop + anchorOffset);
                verts.Add(edges[i - 1].AaTop + anchorOffset);
                verts.Add(edges[i - 1].Top + anchorOffset);
                verts.Add(edges[i].Top + anchorOffset);
                verts.Add(edges[i].Top + anchorOffset);
                verts.Add(edges[i - 1].Top + anchorOffset);
                verts.Add(edges[i - 1].Bottom + anchorOffset);
                verts.Add(edges[i].Bottom + anchorOffset);
                verts.Add(edges[i].Bottom + anchorOffset);
                verts.Add(edges[i - 1].Bottom + anchorOffset);
                verts.Add(edges[i - 1].AaBottom + anchorOffset);
                verts.Add(edges[i].AaBottom + anchorOffset);
            }

            //Subsequent Lines
            for (int i = 2; i < this.Points.Length; i++)
            {
                aaBot = this.Points[i - 1] + curPerp * this.Width;
                aaTop = this.Points[i - 1] - curPerp * this.Width;
                top = this.Points[i - 1] - curPerp * (this.Width - 2 * this.AaWidth);
                bot = this.Points[i - 1] + curPerp * (this.Width - 2 * this.AaWidth);
                edges.Add(new LineEdge(aaBot, bot, top, aaTop));
                aaBot = this.Points[i] + curPerp * this.Width;
                aaTop = this.Points[i] - curPerp * this.Width;
                top = this.Points[i] - curPerp * (this.Width - 2 * this.AaWidth);
                bot = this.Points[i] + curPerp * (this.Width - 2 * this.AaWidth);
                edges.Add(new LineEdge(aaBot, bot, top, aaTop));

            }

            this.Vertices = verts.ToArray();
            mChanged = true;
        }
    }
    override public void OnFill(BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
    {
        if (this.Vertices != null)
        {
            Vector2 uv0 = new Vector2(mOuterUV.xMin + mOuterUV.width / 2, mOuterUV.yMin + mOuterUV.height / 2);
            Vector2 uv1 = new Vector2(mOuterUV.xMax - mOuterUV.width / 2, mOuterUV.yMax - mOuterUV.height / 2);
            Color transparent = this.color;
            transparent.a = 0.0f;

            for (int i = 0 ; i < this.Vertices.Length ; i += 12)
            {
                //Middle
                verts.Add(this.Vertices[i + 4]);
                verts.Add(this.Vertices[i + 5]);
                verts.Add(this.Vertices[i + 6]);
                verts.Add(this.Vertices[i + 7]);

                uvs.Add(uv1);
                uvs.Add(new Vector2(uv1.x, uv0.y));
                uvs.Add(uv0);
                uvs.Add(new Vector2(uv0.x, uv1.y));

                cols.Add(color);
                cols.Add(color);
                cols.Add(color);
                cols.Add(color);

                //Top Edge
                verts.Add(this.Vertices[i]);
                verts.Add(this.Vertices[i + 1]);
                verts.Add(this.Vertices[i + 2]);
                verts.Add(this.Vertices[i + 3]);

                uvs.Add(uv1);
                uvs.Add(new Vector2(uv1.x, uv0.y));
                uvs.Add(uv0);
                uvs.Add(new Vector2(uv0.x, uv1.y));

                cols.Add(transparent);
                cols.Add(transparent);
                cols.Add(color);
                cols.Add(color);

                //Bottom Edge
                verts.Add(this.Vertices[i + 8]);
                verts.Add(this.Vertices[i + 9]);
                verts.Add(this.Vertices[i + 10]);
                verts.Add(this.Vertices[i + 11]);

                uvs.Add(uv1);
                uvs.Add(new Vector2(uv1.x, uv0.y));
                uvs.Add(uv0);
                uvs.Add(new Vector2(uv0.x, uv1.y));

                cols.Add(color);
                cols.Add(color);
                cols.Add(transparent);
                cols.Add(transparent);
            }
        }
    }
    #endregion UIWidget Overrides

    #region Private Methods
    private static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
    {

        return Mathf.Atan2(

            Vector3.Dot(n, Vector3.Cross(v1, v2)),

            Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;

    }
    #endregion Private Methods
}

#region Internal Classes
internal class LineEdge
{
    #region Members
    private Vector2 m_aaBottom;
    private Vector2 m_bottom;
    private Vector2 m_top;
    private Vector2 m_aaTop;
    #endregion Members

    #region Properties
    public Vector2 AaBottom
    {
        get { return this.m_aaBottom; }
        private set { this.m_aaBottom = value; }
    }
    public Vector2 Bottom
    {
        get { return this.m_bottom; }
        private set { this.m_bottom = value; }
    }
    public Vector2 Top
    {
        get { return this.m_top; }
        private set { this.m_top = value; }
    }
    public Vector2 AaTop
    {
        get { return this.m_aaTop; }
        private set { this.m_aaTop = value; }
    }
    #endregion Properties

    #region Constructors
    public LineEdge(Vector2 aaBot, Vector2 bot, Vector2 top, Vector2 aaTop)
    {
        this.AaBottom = aaBot;
        this.Bottom = bot;
        this.AaTop = aaTop;
        this.Top = top;
    }
    #endregion Constructors
}
#endregion Internal Classes
