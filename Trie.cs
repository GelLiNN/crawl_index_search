using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class Trie
    {
        public Node root;
        //end of pattern marker stored as class constant
        public const char MARKER = '#';

        //Constructs a Trie with dummy root node
        public Trie()
        {
            this.root = new Node('0');
        }

        //Adds title to existing Trie data structure
        public void AddTitle(string title)
        {
            title = title.ToLower() + MARKER;
            Node curNode = root;

            foreach (char letter in title)
            {
                curNode = AddHelper(letter, curNode);
            }
        }

        public Node AddHelper(char letter, Node cur)
        {
            Node other = cur.getChild(letter);

            if (other == null)
            {
                Node child = new Node(letter);
                cur.children.Add(child);
                return child;
            }
            else
            {
                return other;
            }
        }

        //Searches Trie for passed prefix matches, returns List of 10 matches
        public List<string> SearchForPrefix(string prefix)
        {
            prefix = prefix.ToLower();
            Node curNode = root;
            foreach (char letter in prefix)
            {
                curNode = AddHelper(letter, curNode);
            }

            List<string> results = new List<string>();
            results.Capacity = 10;

            SearchHelper(curNode, results, prefix);

            return results;
        }

        private void SearchHelper(Node curNode, List<string> results, string prefix)
        {
            foreach (Node child in curNode.children)
            {
                // check if list of words is full
                if (results.Count() < results.Capacity)
                {
                    if (child.value == MARKER)
                    {
                        results.Add(prefix);
                    }
                    else
                    {
                        SearchHelper(child, results, prefix + child.value);
                    }
                }
            }
        }
    }
}
