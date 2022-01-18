using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "RenderPipeline/ToyRenderPipeline")]
public class ToyRenderPipelineAsset : RenderPipelineAsset {

    public Cubemap diffuseIBL;
    public Cubemap specularIBL;
    public Texture brdfLut;

    protected override RenderPipeline CreatePipeline() {
      ToyRenderPipeline rp = new ToyRenderPipeline();
      
      rp.diffuseIBL = diffuseIBL;
      rp.specularIBL = specularIBL;
      rp.brdfLut = brdfLut;

      return rp;
  }
}
