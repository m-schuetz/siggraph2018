#version 450

// 
// Used for SIGGRAPH 2018 Poster "Progressive Real-Time Rendering of Unprocessed Point Clouds"
// 
// authors: Markus Schuetz, Michael Wimmer
// affiliation: TU Wien, Institute of Visual Computing & Human-Centered Technology
// 
// license: BSD 2-Clause, see https://opensource.org/licenses/BSD-2-Clause
//
//
// This compute shader writes indices of visible points to an index buffer object
//

layout(local_size_x = 8, local_size_y = 8) in;

// the FBO color attachment that contains the point indices
layout(rgba8ui, binding = 0) uniform uimage2D uIndices;

// the arguments for glDrawElementsIndirect
layout(std430, binding = 1) buffer ssIndirectCommand{
    uint count;
	uint primCount;
	uint firstIndex;
	uint baseVertex;
	uint baseInstance;
};

// the index buffer object where indices of visible nodes are stored
layout(std430, binding = 3) buffer ssIndices{
	uint indices[];
};

void main() {
	
	uvec2 id = gl_LocalInvocationID.xy + gl_WorkGroupSize.xy * gl_WorkGroupID.xy;
	ivec2 pixelCoords = ivec2(id);
	
	uvec4 vVertexID = imageLoad(uIndices, pixelCoords);
	
	// check if index is not empty (kind of wrong though, also returns at gl_VertexID == 0
	if(vVertexID.r == 0 && vVertexID.g == 0 && vVertexID.b == 0 && vVertexID.a == 0){
		return;
	}

	uint vertexID = vVertexID.r | (vVertexID.g << 8) | (vVertexID.b << 16) | (vVertexID.a << 24);
		
	uint counter = atomicAdd(count, 1);
	
	indices[counter] = vertexID;
	
}






