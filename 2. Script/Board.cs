﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class Board : UITable 
{
    const int column = 6;
    const int row = 5;
    //단순히 UITable에서 Start를 쓰고 있기떄문에 Awake를 씀...
    void Awake()
    {
        for (int n = 0; n < transform.childCount; n++)
        {
            Transform child = transform.GetChild(n);
            child.transform.name = "Element" + n;
            child.GetComponent<Element>().coord = CopyPDTool.IndexToCoord(n);
        }
    }
   
    protected override void Sort(List<Transform> list)
    {
        list.Sort(compareCoord);
    }
    public UISprite uiSprite { get { return GetComponent<UISprite>(); } }
    public int compareCoord(Transform a, Transform b)
    {         //a가 작으면 -1 같으면 버그 a가 크면 1을 리턴하면 될듯ㅇㅇ;     
            Vector2 aCoord = a.GetComponent<Element>().coord;
            Vector2 bCoord = b.GetComponent<Element>().coord;
            return CopyPDTool.CoordToIndex(aCoord).CompareTo(CopyPDTool.CoordToIndex(bCoord));
    }
    public bool bReposition { get { return mReposition; } set { mReposition = value; } }

    //이 List에 등록된 Element는 Reposition 작업시 위치를 변경하지 않는다.
    public List<Element> IgnoreReposition = new List<Element>();
    public override void Reposition()
    {
        //Debug.Log("재배열 실행");
        Vector3[] temp = new Vector3[IgnoreReposition.Count];
        for (int n = 0; n < IgnoreReposition.Count; n++ )
        {   //Reposition 하기전에 좌표를 저장한다.
            temp[n] = IgnoreReposition[n].transform.localPosition;
        }
        base.Reposition();        
        for (int n = 0; n < IgnoreReposition.Count; n++)
        {   //Reposition 된 좌표를 되돌린다.
            IgnoreReposition[n].transform.localPosition = temp[n];
        }
    }
    //유틸리티 함수, 보드의 넓이와 높이가 필요해서 Board의 맴버함수로 넣었다.
    public Vector3 CoordToScreenPosition(Vector2 coord)
    {
        float blockWidth = uiSprite.width / column;
        float blockHeight = uiSprite.height / row;
        return new Vector3(blockWidth / 2 + blockWidth * coord.x, -blockHeight / 2 - blockHeight * coord.y, 0);
    }
    
    public Element getElement(Vector2 coord)
    {
        return children[CopyPDTool.CoordToIndex(coord)].GetComponent<Element>();
    }
    public Element getElement(int x,int y)
    {
        return children[CopyPDTool.CoordToIndex(x,y)].GetComponent<Element>();
    }

    //파괴시킬 엘리먼트
    public List<Element> listDestroy = new List<Element>();
    void DectectionColumn()
    {       
        List<Element> tempArray = new List<Element>();     
        for (int x = 0; x < 6; x++)
        {  
            for (int y = 0; y < 5; y++)
            {   //5x6 모든 원소를 종주함      
                if(tempArray.Count == 0)
                {   
                    tempArray.Add(getElement(x, y));
                    continue;
                }
                if(tempArray[0].type != getElement(x,y).type)
                {          
                    if (tempArray.Count >= 3)
                    {         
                        foreach (Element a in tempArray)
                        {
                            a.listGroup = tempArray.GetRange(0, tempArray.Count);
                            listDestroy.Add(a);
                        }
                    }               
                    tempArray.Clear();
                }    
                tempArray.Add(getElement(x, y)); 
            }
            if (tempArray.Count >= 3)
            {
                foreach (Element a in tempArray)
                {
                    a.listGroup = tempArray.GetRange(0, tempArray.Count);
                    listDestroy.Add(a);
                }
            }
            tempArray.Clear();
        }
    }
    void DectectionRow()
    {  
        List<Element> tempArray = new List<Element>();
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                if (tempArray.Count == 0)
                {
                    tempArray.Add(getElement(x, y));
                    continue;
                }

                if (tempArray[0].type != getElement(x, y).type)
                {
                    if (tempArray.Count >= 3)
                    {                                       
                        foreach (Element a in tempArray)
                        {  
                           if(!listDestroy.Contains(a))
                           {
                               a.listGroup = tempArray.GetRange(0, tempArray.Count);
                               listDestroy.Add(a);                                         
                           }
                        }
                    }
                    tempArray.Clear();
                }
                tempArray.Add(getElement(x, y));
            }
            if (tempArray.Count >= 3)
            {

                foreach (Element a in tempArray)
                {
                    if (!listDestroy.Contains(a))
                    {
                        a.listGroup = tempArray.GetRange(0, tempArray.Count);
                          listDestroy.Add(a);
                    }
                }
            }
            tempArray.Clear();
        }
//        Debug.Log(listDestroy.Count + "개의 원소 추가");
    }

    IEnumerator endSequence()
    {
        GameCore.instance.combo = 0;
        //입력 막음
        while (true)
        {  
            //세로감지
            DectectionColumn();
            //가로감지
            DectectionRow();
           
            if (listDestroy.Count == 0)
                break;//만약 파괴할게 없다면 빠져나옴
            
            foreach (Element a in listDestroy) //파괴될 엘리먼트가 메세지를 보냄.
            {   a.SendDropMessage();            
            }

           
            foreach (Element a in listDestroy)
            {   //if(!listDead.Contains(a))
                yield return StartCoroutine(a.Dead(0.1f));
                GameCore.instance.combo += 1;
            }
            //yield return new WaitForSeconds(0.5f);      
                    
            foreach (Transform a in children)
            {   if (!listDestroy.Contains(a.GetComponent<Element>()))
                {   //파괴하지 않은 드롭들을 Drop 만큼 좌표를 내리고 이동시킴
                    a.GetComponent<Element>().dropCoord();
                    StartCoroutine(a.GetComponent<Element>().MoveToMyPosition(0.3f));
                }
            }
            yield return new WaitForSeconds(0.3f);         
            foreach (Element a in listDestroy)
            {   //드롭을 새로 생성함
                StartCoroutine( a.Alive(0.5f));                
            }
            yield return new WaitForSeconds(0.5f);
            foreach (Element a in listDestroy)
            {   //새로 생성된 Element들을 이동시킴
                StartCoroutine(a.MoveToMyPosition(0.3f));
            }          
            yield return new WaitForSeconds(0.3f);
            children.Sort(compareCoord);
            listDestroy.Clear();    
        }
        GameCore.instance.combo = 0;
    }
    public Coroutine DectectDestoryElement()
    {
        return StartCoroutine(endSequence());          
    }

    //강의5 시작
    public void OrderToSendMessage()
    {
        foreach(Element a in listDestroy)
        {
            a.SendDropMessage();
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Board))]
public class BoardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        //UITable에서 Reposition을 버튼으로 사용하게 만듬
        Board myScript = target as Board;
        if (GUILayout.Button("Reposition Button"))
        {
           myScript.Reposition();
        }
    }
}
#endif