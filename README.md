# ToyRenderPipeline

An custom Render Pipeline with Unity SRP（Scriptable Render Pipeline）

![image-20220118141514668](README.assets/image-20220118141514668.png)

![image-20220118141333385](README.assets/image-20220118141333385.png)



# Features

* Deferred Shadings Pipeline
* Physical Based Rendering（PBR，direct light）
* Image Based Lighting（IBL，ambient light）
* glTF Material support



# Environment

* Unity 2021.2，win 11，DX 11
* [UniGLTF](https://github.com/ousttrue/UniGLTF) package（alternative，you can config material in Unity Inspector）
* MRT supporting GPU



# Usage

* Menu `Create > RenderPipeline > ToyRenderPipeline`，create a pipeline asset
* Menu `Edit > Project Settings > Graphics`，select the custom pipeline
* Bind `ToyRP/gbuffer` shader to all materials



# Compare With Unity 

Using some tricks trying to align the rendering result to Unity Build-in Pipeline，left column is IBL effect，while right is direction light

![image-20220118142219780](README.assets/image-20220118142219780.png)



# Todo

- [ ] Shadow Mapping

- [ ] Cluster Shading
- [ ] Screen Space Effect
- [ ] Volumn Effect
- [ ] Post Process



# Reference

[1]  JoeyDeVries, ["Learn OpenGL"](https://learnopengl-cn.github.io/07%20PBR/01%20Theory/)

[2] 王江荣, ["【Unity】SRP简单入门"](https://zhuanlan.zhihu.com/p/378828898)

[3] 銀葉吉祥, ["一起来写Unity渲染管线吧！"](https://zhuanlan.zhihu.com/p/35862626)

[4] MaxwellGeng, ["在Unity里写一个纯手动的渲染管线"](https://zhuanlan.zhihu.com/p/43588045)

[5] Catlike Coding, ["Custom Render Pipeline"](https://catlikecoding.com/unity/tutorials/custom-srp/custom-render-pipeline/)

[6] Unity Manual, ["Scriptable Render Pipeline"](https://docs.unity3d.com/2021.2/Documentation/Manual/ScriptableRenderPipeline.html)

[7] 郭大钦, ["unity build-in管线中的PBR材质Shader分析研究"](https://blog.csdn.net/qq_23936433/article/details/108507640)

[8] 雨轩, ["Unity PBR Standard Shader 实现详解"](https://zhuanlan.zhihu.com/p/137039291)

[9] taecg, ["零基础入门Unity Shader"](https://zhuanlan.zhihu.com/p/46745694)

[10] Tim Cooper, ["Scriptable Render Pipeline Overview"](https://blog.unity.com/technology/srp-overview)

[11] LetMe_See, ["Unity中的CommandBuffer"](https://www.jianshu.com/p/aa9f31e2e345)

[12] EA DICE, ["Moving Frostbite to PBR"](https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf)

[13] cinight, ["CustomSRP"](https://github.com/cinight/CustomSRP)

[14] moriya苏蛙可, ["DX12渲染管线(1) - 基于物理的渲染(PBR)"](https://zhuanlan.zhihu.com/p/61962884)

[15] pema99, ["shader-knowledg"](https://github.com/pema99/shader-knowledge/blob/26f609ebd68d0128a268a528e822d15111de354d/mrt.md)

[16] dariomanesku, ["cmftStudio"](https://github.com/dariomanesku/cmftStudio)

