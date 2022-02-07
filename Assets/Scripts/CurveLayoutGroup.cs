using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CurveLayoutGroup : LayoutGroup
{
    public enum GenerateType
    {
        All,        //总体模式    
        Interval,   //间隔模式
        CurveVertex //曲线顶点模式
    }

    protected CurveLayoutGroup() { }
    public AnimationCurve curve;
    public float curveX_Factor = 100;
    public float curveY_Factor = 100;

    public Vector2 StartPos = Vector2.zero;

    /// <summary>
    /// 子节点生成类型
    /// </summary>
    public GenerateType Type = GenerateType.All;

    #region 间隔模式
    [Range(2, 100)]
    public int NodeCount = 10;

    #endregion


    private float curveLength
    {
        get
        {
            if (this.curve.length == 0)
                return 1f;
            return this.curve.keys[this.curve.length - 1].time - this.curve.keys[0].time;
        }
    }

    private float curveHeight
    {
        get
        {
            float max = float.MinValue;
            float min = float.MaxValue;
            foreach (var item in this.curve.keys)
            {
                if (max < item.value)
                    max = item.value;
                if (min > item.value)
                    min = item.value;
            }
            return max - Mathf.Min(0, min);
        }
    }

    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        CalcAlongAxis(0);
    }

    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void CalculateLayoutInputVertical()
    {
        CalcAlongAxis(1);
    }

    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void SetLayoutHorizontal()
    {

        switch (this.Type)
        {
            case GenerateType.All:
                SetChildrenAlongAxis_All(0);
                break;
            case GenerateType.Interval:
                SetChildrenAlongAxis_Type(0);
                break;
            case GenerateType.CurveVertex:
                SetChildrenAlongAxis_Curve(0);
                break;
            default:
                break;
        }
    }



    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void SetLayoutVertical()
    {
        switch (this.Type)
        {
            case GenerateType.All:
                SetChildrenAlongAxis_All(1);
                break;
            case GenerateType.Interval:
                SetChildrenAlongAxis_Type(1);
                break;
            case GenerateType.CurveVertex:
                SetChildrenAlongAxis_Curve(1);
                break;
            default:
                break;
        }
    }


    private void CalcAlongAxis(int axis)
    {
        if (this.curve == null)
            return;

        float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);

        float totalMin = combinedPadding;
        float totalPreferred = combinedPadding;
        float totalFlexible = 0;

        if(rectChildren.Count == 0)
        {

        }
        else if(rectChildren.Count == 1)
        {
            totalMin += rectChildren[0].sizeDelta[axis] * rectChildren[0].localScale[axis];
            totalPreferred += rectChildren[0].sizeDelta[axis] * rectChildren[0].localScale[axis];
        }
        else
        {
            totalMin += rectChildren[0].sizeDelta[axis] * rectChildren[0].pivot[axis] * rectChildren[0].localScale[axis];
            totalPreferred += rectChildren[0].sizeDelta[axis] * rectChildren[0].pivot[axis] * rectChildren[0].localScale[axis];

            totalMin += rectChildren[rectChildren.Count - 1].sizeDelta[axis] * (1 - rectChildren[rectChildren.Count - 1].pivot[axis]) * rectChildren[rectChildren.Count - 1].localScale[axis];
            totalPreferred += rectChildren[rectChildren.Count - 1].sizeDelta[axis] * (1 - rectChildren[rectChildren.Count - 1].pivot[axis]) * rectChildren[rectChildren.Count - 1].localScale[axis];
        }

        totalMin += (axis == 0 ? curveX_Factor * this.curveLength : curveY_Factor * this.curveHeight);
        totalPreferred += (axis == 0 ? curveX_Factor * this.curveLength : curveY_Factor * this.curveHeight) + combinedPadding;


        totalPreferred = Mathf.Max(totalMin, totalPreferred);
        //Debug.Log($"All  totalMin: {totalMin}    totalPreferred: {totalPreferred}   totalFlexible: {totalFlexible}    axis: {axis}");
        SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
    }

    private void SetChildrenAlongAxis_All(int axis)
    {
        if (this.curve == null)
            return;

        if (this.rectChildren.Count == 0)
            return;

        float size = rectTransform.rect.size[axis];                                                                                                                                                                                                                             
        float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
        float factor = axis == 0 ? this.curveX_Factor : this.curveY_Factor;
        float paddingOffset = axis == 0 ? padding.left : padding.top;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            float scaleFactor = child.localScale[axis];
            float param = axis == 0 ? this.CurveXAxis(this.MappingValue(i)) : -(this.curve.Evaluate(this.CurveXAxis(this.MappingValue(i))));

            float startOffset = GetStartOffset(axis, 0) + factor * param * child.localScale[axis];
            startOffset += paddingOffset + (axis == 0 ? StartPos.x : StartPos.y);
            SetChildAlongAxisWithScale(child, axis, startOffset, scaleFactor);
        }
    }

    private void SetChildrenAlongAxis_Type(int axis)
    {
        if (this.curve == null)
            return;

        if (this.rectChildren.Count == 0)
            return;

        if(this.rectChildren.Count > this.NodeCount)
        {
            Debug.LogError($"子节点的数量为{this.rectChildren.Count}不可大于节点数量{this.NodeCount}");
            return;
        }

        float size = rectTransform.rect.size[axis];
        float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
        float factor = axis == 0 ? this.curveX_Factor : this.curveY_Factor;
        float paddingOffset = axis == 0 ? padding.left : padding.top;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            float scaleFactor = child.localScale[axis];
            float param = axis == 0 ? this.CurveXAxis(i/(float)(NodeCount - 1)) : -(this.curve.Evaluate(this.CurveXAxis(i / (float)(NodeCount - 1))));

            float startOffset = GetStartOffset(axis, 0) + factor * param * child.localScale[axis];
            startOffset += paddingOffset + (axis == 0 ? StartPos.x : StartPos.y);
            SetChildAlongAxisWithScale(child, axis, startOffset, scaleFactor);
        }
    }

    private void SetChildrenAlongAxis_Curve(int axis)
    {
        if (this.curve == null)
            return;

        if (this.rectChildren.Count == 0)
            return;

        if(this.curve.keys.Length < 2)
        {
            Debug.LogError($"曲线顶点数不可小于2");
            return;
        }

        if (this.rectChildren.Count > this.curve.keys.Length)
        {
            Debug.LogError($"子节点的数量为{this.rectChildren.Count}不可大于节点数量{this.curve.keys.Length}");
            return;
        }

        float size = rectTransform.rect.size[axis];
        float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
        float factor = axis == 0 ? this.curveX_Factor : this.curveY_Factor;
        float paddingOffset = axis == 0 ? padding.left : padding.top;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            float scaleFactor = child.localScale[axis];
            float param = axis == 0 ? this.curve.keys[i].time : -this.curve.keys[i].value;

            float startOffset = GetStartOffset(axis, 0) + factor * param * child.localScale[axis];
            startOffset += paddingOffset + (axis == 0 ? StartPos.x : StartPos.y);
            SetChildAlongAxisWithScale(child, axis, startOffset, scaleFactor);
        }
    }

    private float CurveXAxis(float rate)
    {
        return rate * curveLength + this.curve.keys[0].time;
    }

    private float MappingValue(int index)
    {
        float rate = (float)rectChildren.Count / (rectChildren.Count - 1);
        return rate * index / rectChildren.Count;
    }




#if UNITY_EDITOR

    private int m_Capacity = 10;
    private Vector2[] m_Sizes = new Vector2[10];

    protected void Update()
    {
        if (Application.isPlaying) return;
        int count = transform.childCount;
        if (count > m_Capacity)
        {
            if (count > m_Capacity * 2)
                m_Capacity = count;
            else
                m_Capacity *= 2;

            m_Sizes = new Vector2[m_Capacity];
        }

        bool dirty = false;
        for (int i = 0; i < count; i++)
        {
            RectTransform t = transform.GetChild(i) as RectTransform;
            if( t!=null && t.sizeDelta != m_Sizes[i])
            {
                dirty = true;
                m_Sizes[i] = t.sizeDelta;
            }
        }

        if (dirty)
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);

    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < rectChildren.Count - 1; i++)
        {
            Gizmos.DrawLine(rectChildren[i].position, rectChildren[i + 1].position);
        }
    }
    //private void OnDrawGizmosSelected()
    //{
    //    for (int i = 0; i < rectChildren.Count - 1; i++)
    //    {
    //        Gizmos.DrawLine(rectChildren[i].anchoredPosition, rectChildren[i+1].anchoredPosition);
    //    }
    //}

#endif
}