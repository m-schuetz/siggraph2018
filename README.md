
# About

This repository contains some source code samples to accompany the SIGGRAPH 2018 Poster __"Progressive Real-Time Rendering of Unprocessed Point Clouds"__. 
The samples are an excerpt of the core components and stripped by smaller, but necessary and verbose, steps. 
We will evaluate a full source release in the future.





```cpp

// add a new batch of points
// this is done in a parallel thread that prepares
// the workload for the actual distribution into the VBO by the main thread.
void addPoints(vector<Point> &newPoints) {
	int size = previouslyAddedPoints;
	int numBuckets = 1 + size / newPoints.size();

	std::random_device r;
	std::default_random_engine re(r());
	std::uniform_int_distribution<int> ud(0, numBuckets - 1);

	vector<int> targetIndices;

	targetIndices.reserve(newPoints.size());

	int i = 0;

	// shuffle points within batch. Not sufficient on its own
	std::random_shuffle(newPoints.begin(), newPoints.end());

	// compute target indices to shuffle points within random batches
	for (auto &point : newPoints) {
		int bucket = ud(re);
		targetIndices.push_back(i + bucket * newPoints.size());

		i++;
	}

	// schedule distribution task for the main thread
	std::lock_guard<std::mutex> guard(addMutex);
	distributeTasks.emplace_back(newPoints, targetIndices, offset);
}
```


```cpp

// does the distribution of the new batch in the VBO in the main thread
void distribute(){

	std::lock_guard<std::mutex> guard(addMutex);

	auto task = distributeTasks.front();
	distributeTasks.pop_front();

	int numPoints = task.points.size();

	static ComputeShader *csDistribute = nullptr;
	if (csDistribute == nullptr) {
		string cs = Utils::loadFileAsString("./resources/shaders/distribute_streaming.cs");

		csDistribute = new ComputeShader(cs);
	}

	glUseProgram(csDistribute->program);

	int offset = task.offset;
	csDistribute->setUniform("uNumPoints", numPoints);
	csDistribute->setUniform("uOffset", offset);

	static GLuint ssInputBuffer = -1;
	static GLuint ssTargetIndices = -1;
	if (ssInputBuffer == -1) {
		glCreateBuffers(1, &ssInputBuffer);
		glCreateBuffers(1, &ssTargetIndices);

		glNamedBufferData(ssInputBuffer, task.points.size() * sizeof(Point), task.points.data(), GL_DYNAMIC_DRAW);
		glNamedBufferData(ssTargetIndices, task.points.size() * sizeof(GLuint), task.targetIndices.data(), GL_DYNAMIC_DRAW);
	} else {
		glNamedBufferSubData(ssInputBuffer, 0, task.points.size() * sizeof(Point), task.points.data());
		glNamedBufferSubData(ssTargetIndices, 0, task.points.size() * sizeof(GLuint), task.targetIndices.data());
	}

	glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 0, ssInputBuffer);
	glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 1, ssTargetIndices);
	glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 2, vbo);

	glDispatchCompute(1 + numPoints / 32, 1, 1);

	glMemoryBarrier(GL_ALL_BARRIER_BITS);

	glUseProgram(0);
}

```