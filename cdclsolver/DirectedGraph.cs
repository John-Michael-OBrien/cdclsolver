using System;
using System.Collections.Generic;
using System.Text;

namespace cdclsolver
{
    class DirectedGraph
    {
        class DirectedGraphNode<NodeType>
        {
            // Property getters and setters
            public NodeType Data { get; set; }
            public HashSet<DirectedGraphNode<NodeType>> ConnectedNodes { get; private set; }

            private void _init_connections(IEnumerable<DirectedGraphNode<NodeType>> connections = null)
            {
                // Make our connection set
                ConnectedNodes = new HashSet<DirectedGraphNode<NodeType>>();
                
                // If we've been handed a list of connections to add
                if (!(connections is null))
                {
                    // Go through the list
                    foreach(DirectedGraphNode<NodeType> connection in connections) {
                        // And add them.
                        ConnectedNodes.Add(connection);                        
                    }
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
