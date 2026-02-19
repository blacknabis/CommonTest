using System;
using System.Collections.Generic;

namespace CatSudoku.Editor
{
    [Serializable]
    public class PromptNode
    {
        public string class_type;
        public Dictionary<string, object> inputs = new Dictionary<string, object>();
    }

    [Serializable]
    public class PromptWorkflow
    {
        public Dictionary<string, PromptNode> prompt = new Dictionary<string, PromptNode>();
    }

    [Serializable]
    public class QueuePromptResponse
    {
        public string prompt_id;
        public int number;
        public Dictionary<string, object> node_errors;
    }

    [Serializable]
    public class HistoryResponse
    {
        public Dictionary<string, HistoryData> History;
    }
    
    [Serializable]
    public class HistoryData
    {
       // outputs is a Dictionary where Key = NodeID (e.g. "9") and Value = NodeOutput
       public Dictionary<string, NodeOutput> outputs;
    }
    
    [Serializable]
    public class NodeOutput
    {
        public ImageOutput[] images;
        public AudioOutput[] audio;
    }

    [Serializable]
    public class ImageOutput
    {
        public string filename;
        public string subfolder;
        public string type;
    }

    [Serializable]
    public class AudioOutput
    {
        public string filename;
        public string subfolder;
        public string type;
    }
}
