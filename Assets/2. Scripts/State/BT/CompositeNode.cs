using System.Collections.Generic;

namespace BT
{
    public abstract class CompositeNode : Node
    {
        protected readonly List<Node> children;

        protected CompositeNode(params Node[] children)
        {
            this.children = new List<Node>(children);
        }
    }
}
