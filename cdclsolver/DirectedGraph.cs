using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    public class DirectedGraph
    {
        public class DirectedGraphNode<NodeType>
        {
            // Property getters and setters
            public NodeType Data { get; set; }
            public HashSet<DirectedGraphNode<NodeType>> ConnectedNodes { get; private set; }

            private void _init_connections(IEnumerable<DirectedGraphNode<NodeType>> connections = null)
            {
                // If we've been handed a list of connections to add
                if (!(connections is null))
                {
                    // Make our connection set with the variables in it.
                    ConnectedNodes = new HashSet<DirectedGraphNode<NodeType>>(connections);
                }
                else
                {
                    // Make a blank connection set
                    ConnectedNodes = new HashSet<DirectedGraphNode<NodeType>>();
                }
            }

            public DirectedGraphNode(IEnumerable<DirectedGraphNode<NodeType>> connections = null) {
                _init_connections(connections);
            }

            public DirectedGraphNode(NodeType new_data, IEnumerable<DirectedGraphNode<NodeType>>  connections = null)
            {
                _init_connections(connections);
                Data = new_data;
            }

        }
    }
}
