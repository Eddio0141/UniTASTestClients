using UnityEngine;

namespace UI
{
    [CreateAssetMenu(fileName = "FILENAME", menuName = "MENUNAME", order = 0)]
    public class Test : ScriptableObject
    {
        public int Value;
        public Object TestObj;
    }
}