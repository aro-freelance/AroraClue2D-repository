using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
///  this is used to alphabetically sort the letters within a string
/// </summary>
/// 

public class SortString : MonoBehaviour
{
  
    public static SortString instance;


    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public string AlphabeticallySortCharOfString(string inputString)
    {
        char[] tempArray = inputString.ToCharArray();

        Array.Sort(tempArray);

        return new string(tempArray);

    }

   


}
