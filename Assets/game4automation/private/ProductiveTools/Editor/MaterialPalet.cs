using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game4automation
{
    [CreateAssetMenu(fileName = "Materialpalet", menuName = "game4automation/Add material collection",
        order = 1)]
    public class MaterialPalet : ScriptableObject
    {
        public List<Material> materiallist;
    }
}
