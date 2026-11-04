using UnityEngine;

public class PolarisApply : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public PolarisReceiver polarisReceiverObject;
    public bool printMatrices = false;
    public bool applyPosition = true;

    public PolarisReceiver.Tool toolToApply = PolarisReceiver.Tool.Probe;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (printMatrices)
        {
            var receiver = polarisReceiverObject.GetComponent<PolarisReceiver>();
            var matrices = receiver.Matrices;
            var visible = receiver.Visible;
            for (int i = 0; i < matrices.Length; i++)
            {
                Debug.Log($"Tool {i}: Visible={visible[i]}, Matrix=\n{matrices[i]}");
            }
        }

        if(applyPosition)
        {
            var receiver = polarisReceiverObject.GetComponent<PolarisReceiver>();
            PolarisReceiver.ToolInfo toolInfo = receiver.GetToolInfo(toolToApply);

            GetComponent<Renderer>().enabled = toolInfo.visible;
            transform.position = toolInfo.matrix.GetColumn(3) * 0.01f; 
            transform.rotation = Quaternion.LookRotation(toolInfo.matrix.GetColumn(2), toolInfo.matrix.GetColumn(1));    
        }
    }
}
