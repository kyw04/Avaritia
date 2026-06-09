namespace BT
{
    public class Selector : CompositeNode
    {
        public Selector(params Node[] children) : base(children) { }

        public override NodeStatus Evaluate()
        {
            foreach (var child in children)
            {
                var status = child.Evaluate();
                if (status != NodeStatus.Failure)
                    return status;
            }
            return NodeStatus.Failure;
        }
    }
}
