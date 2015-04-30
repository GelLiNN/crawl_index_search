using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class Node
    {
        public char value;
        public List<Node> children;

        public Node(char newval)
        {
            this.value = newval;
            this.children = new List<Node>();
        }

        public Node getChild(char testval)
        {
            foreach (Node child in children)
            {
                if (child.value == testval)
                {
                    return child;
                }
            }
            return null;
        }
    }
}
