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
// This compute shader randomly distributes a new batch of points in a large VBO
// Random target locations are generated on the CPU.
// Target locations are unique to avoid race conditions.
//



layout(local_size_x = 32, local_size_y = 1) in;

struct Vertex{
	float ux;
	float uy;
	float uz;
	uint colors;
};

// This buffer contains points of the newly loaded batch,
// which are going to be distributed to the main VBO by this shader
layout(std430, binding = 0) buffer ssInputBuffer{
	Vertex inputBuffer[];
};

// contains the target location in the main VBO for each point in the new batch
layout(std430, binding = 1) buffer ssTargetIndices{
	uint targetIndices[];
};

// the main VBO. the new batch is distributed over this buffer
layout(std430, binding = 2) buffer ssTargetBuffer{
	Vertex targetBuffer[];
};

// number of points in the new batch / batchSize
uniform int uNumPoints;

// first unused location in the main VBO
// aka the number of previously added points, excluding the new batch
uniform int uOffset;

void main(){
	
	uint workGroupSize = gl_WorkGroupSize.x * gl_WorkGroupSize.y;

	// [0, batchSize) == [0, uNumPoints)
	uint inputIndex = gl_WorkGroupID.x * workGroupSize 
		+ gl_WorkGroupID.y * gl_NumWorkGroups.x * workGroupSize
		+ gl_LocalInvocationIndex;
		
	if(inputIndex >= uNumPoints){
		return;
	}
	
	uint sequentialIndex = uOffset + inputIndex;
	uint targetIndex = targetIndices[inputIndex];

	// If there is already a point at the target location, 
	// move that point to a free spot at the end first
	if(targetIndex < uOffset){
		targetBuffer[sequentialIndex] = targetBuffer[targetIndex];
	}
	
	targetBuffer[targetIndex] = inputBuffer[inputIndex];

}



