# AI RAG strategies
Comprehensive list of strategies to enhance your LLM's ability to reason about your private data.
How and when to implement various LLM memory structures and patterns on Azure Services.
Patterns for enhancing the accuracy, efficiency, and contextual richness of RAG systems.


## What this doesn't do

- **Provide a RAG platform**
- Explain vector databases

### Understand Index Type and Distance Functions

#### **1. `Vector Index Type`**

This option determines how vectors are indexed within Cosmos DB to optimize search performance.
<details>
<summary>
Options
</summary>

- **`flat`**: Stores vectors alongside other indexed properties without additional indexing structures. Supports up to **505 dimensions**.

  **When to Use:**

  - **Low-dimensional data**: Ideal for applications with vectors up to 505 dimensions.
  - **Exact search requirements**: When you need precise search results.
  - **Small to medium datasets**: Efficient for datasets where the index size won't become a bottleneck.

  **Real-World Scenario:**

  - **Customer Segmentation**: A retail company uses customer feature vectors (age, income, purchase history) with dimensions well below 505 to segment customers. Exact matches are important for targeted marketing campaigns.

- **`quantizedFlat`**: Compresses (quantizes) vectors before indexing, improving performance at the cost of some accuracy. Supports up to **4096 dimensions**.

  **When to Use:**

  - **High-dimensional data with storage constraints**: Suitable for vectors up to 4096 dimensions where storage efficiency is important.
  - **Performance-critical applications**: When reduced latency and higher throughput are needed.
  - **Acceptable accuracy trade-off**: Minor losses in accuracy are acceptable for performance gains.

  **Real-World Scenario:**

  - **Mobile Image Recognition**: An app recognizes objects using high-dimensional image embeddings. Quantization reduces the storage footprint and improves search speed, crucial for mobile devices with limited resources.

- **`diskANN`**: Utilizes the DiskANN algorithm for approximate nearest neighbor searches, optimized for speed and efficiency. Supports up to **4096 dimensions**.

  **When to Use:**

  - **Large-scale, high-dimensional data**: Best for big datasets where quick approximate searches are acceptable.
  - **Real-time applications**: When fast response times are critical.
  - **Scalability needs**: Suitable for applications expected to grow significantly.

  **Real-World Scenario:**

  - **Semantic Search Engines**: A search engine indexes millions of documents using embeddings from language models like BERT (768 dimensions). DiskANN allows users to get fast search results by efficiently handling high-dimensional data.
</details>

---

#### **2. `Vector Data Type`**

Specifies the data type of the vector components.
<details>
<summary>Options</summary>

- **`float32`** (default): 32-bit floating-point numbers.

  **When to Use:**

  - **High precision requirements**: Necessary when the application demands precise calculations.
  - **Standard ML embeddings**: Most machine learning models output float32 vectors.

  **Real-World Scenario:**

  - **Scientific Simulations**: In climate modeling, vectors represent complex data where precision is vital for accurate simulations and predictions.

- **`uint8`**: 8-bit unsigned integers.

  **When to Use:**

  - **Memory optimization**: Reduces storage needs when precision can be sacrificed.
  - **Quantized models**: When vectors are output from models that already quantize data.

  **Real-World Scenario:**

  - **Basic Image Features**: Storing color histograms for image retrieval systems, where each bin can be represented with an 8-bit integer.

- **`uint8`**: 8-bit integer with potentially specialized encoding (interpretation may vary; assuming it's an 8-bit integer with logarithmic encoding).

  **When to Use:**

  - **Custom quantization schemes**: When using specialized compression techniques that map floating-point values to an 8-bit integer scale.
  - **Edge devices**: Ideal for applications on devices with extreme memory limitations.

  **Real-World Scenario:**

  - **Audio Fingerprinting**: Compressing audio feature vectors for song recognition apps where storage and quick retrieval are essential.
</details>

---
#### **3. `Dimension Size`**

The length of the vectors being indexed. Ranges from 0-4096, default is **1536**.
<details>
<summary>Options</summary>


**When to Consider Lower Dimensions (≤ 505):**

  - **Simpler models**: Applications using basic embeddings or feature vectors.
  - **Flat index type**: Required when using the `flat` index type due to its dimension limit.

  *Real-World Scenario:*

  - **Keyword Matching**: Using low-dimensional TF-IDF vectors for document similarity in a content management system.

  **When to Consider Higher Dimensions (506 - 4096):**

  - **Complex models**: Deep learning applications with high-dimensional embeddings.
  - **Advanced search features**: When richer representations of data are necessary for accuracy.

  *Real-World Scenario:*

  - **Face Recognition**: Using high-dimensional embeddings (e.g., 2048 dimensions) to represent facial features for security systems.
</details>

---

#### **4. `Distance Function`**

Determines how similarity between vectors is calculated.
<details>
<summary>Options</summary>

- **`cosine`**: Measures the cosine of the angle between vectors.

  **When to Use:**

  - **Orientation-focused similarity**: When the magnitude is less important than the direction.
  - **Normalized data**: Ideal when vectors are normalized to unit length.

  **Real-World Scenario:**

  - **Document Similarity**: In text analytics, comparing documents based on topic similarity where word counts are normalized.

- **`dot product`**: Computes the scalar product of two vectors.

  **When to Use:**

  - **Magnitude matters**: When both direction and magnitude are significant.
  - **Machine learning models**: Often used in recommendation systems where strength of preferences is important.

  **Real-World Scenario:**

  - **Personalized Recommendations**: Matching users to products by calculating the dot product of user and item embeddings in a collaborative filtering system.

- **`euclidean`**: Calculates the straight-line distance between vectors.

  **When to Use:**

  - **Spatial distance relevance**: When physical distance correlates with similarity.
  - **High-dimensional data**: Suitable for embeddings where both magnitude and direction impact similarity.

  **Real-World Scenario:**

  - **Anomaly Detection**: Identifying outliers in network traffic patterns by measuring Euclidean distances in feature space.

---

### **CosmosDB: Option Combinations and Their Preferred Use-Cases**

#### **Combination 1: Low-Dimensional, Exact Searches**

- **`vectorIndexType`**: `flat`
- **`datatype`**: `float32`
- **`dimensions`**: ≤ 505
- **`distanceFunction`**: `cosine`

**Real-World Scenario:**

- **Small-Scale Text Classification**: A startup builds a news categorization tool using word embeddings (300 dimensions). Exact cosine similarity searches ensure accurate article tagging without the overhead of approximate methods.

---

#### **Combination 2: High-Dimensional, Performance-Critical Applications**

- **`vectorIndexType`**: `diskANN`
- **`datatype`**: `float32`
- **`dimensions`**: 768 - 1536
- **`distanceFunction`**: `cosine` or `dot product`

**Real-World Scenario:**

- **Real-Time Recommendations**: A streaming service uses user and content embeddings (1024 dimensions) to provide instantaneous movie recommendations. DiskANN accelerates search times, offering a smooth user experience despite the large dataset.

---

#### **Combination 3: Storage-Efficient High-Dimensional Data**

- **`vectorIndexType`**: `quantizedFlat`
- **`datatype`**: `uint8` or `iln8`
- **`dimensions`**: 2048
- **`distanceFunction`**: `cosine`

**Real-World Scenario:**

- **Mobile Visual Search**: An app allows users to search for products by uploading photos. High-dimensional image embeddings are quantized to fit the storage constraints of mobile devices, and approximate searches provide quick results.

---

#### **Combination 4: Precision-Critical Scientific Computing**

- **`vectorIndexType`**: `flat`
- **`datatype`**: `float32`
- **`dimensions`**: 4096
- **`distanceFunction`**: `euclidean`

**Real-World Scenario:**

- **Genomic Data Analysis**: Researchers analyze genetic sequences represented as high-dimensional vectors. Precise Euclidean distance calculations are essential for identifying genetic similarities and mutations.

---

#### **Combination 5: Medium-Dimensional Data with Storage Constraints**

- **`vectorIndexType`**: `quantizedFlat`
- **`datatype`**: `uint8`
- **`dimensions`**: 500
- **`distanceFunction`**: `dot product`

**Real-World Scenario:**

- **IoT Sensor Data**: A network of sensors generates medium-dimensional vectors representing environmental data. Quantization reduces storage and transmission costs, and dot product calculations help in identifying patterns and anomalies efficiently.

---

### **Summary**

- **`flat` Index Type**: Use for low-dimensional, exact searches on smaller datasets.
- **`quantizedFlat` Index Type**: Choose when you need to balance performance and storage with acceptable accuracy loss in high-dimensional data.
- **`diskANN` Index Type**: Opt for large-scale, high-dimensional datasets where approximate searches suffice, and speed is critical.
- **`float32` Datatype**: Default choice for precision; use when storage is less of a concern.
- **`uint8` and `iln8` Datatypes**: Use for storage efficiency, particularly when data can be quantized.
- **Dimensions**: Match the dimensionality to your data and index type constraints.
- **Distance Functions**: Select based on the nature of similarity in your application—`cosine` for orientation, `dot product` when magnitude matters, and `euclidean` for spatial relevance.


Certainly! Let's break down each of the options available when defining a Vector Profile in Azure Cosmos DB and explore real-world scenarios where each option or combination would be preferred.

---

### **1. `vectorIndexType`**

This option determines how vectors are indexed within Cosmos DB to optimize search performance.

#### **Options:**

- **`flat`**: Stores vectors alongside other indexed properties without additional indexing structures. Supports up to **505 dimensions**.

  **When to Use:**

  - **Low-dimensional data**: Ideal for applications with vectors up to 505 dimensions.
  - **Exact search requirements**: When you need precise search results.
  - **Small to medium datasets**: Efficient for datasets where the index size won't become a bottleneck.

  **Real-World Scenario:**

  - **Customer Segmentation**: A retail company uses customer feature vectors (age, income, purchase history) with dimensions well below 505 to segment customers. Exact matches are important for targeted marketing campaigns.

- **`quantizedFlat`**: Compresses (quantizes) vectors before indexing, improving performance at the cost of some accuracy. Supports up to **4096 dimensions**.

  **When to Use:**

  - **High-dimensional data with storage constraints**: Suitable for vectors up to 4096 dimensions where storage efficiency is important.
  - **Performance-critical applications**: When reduced latency and higher throughput are needed.
  - **Acceptable accuracy trade-off**: Minor losses in accuracy are acceptable for performance gains.

  **Real-World Scenario:**

  - **Mobile Image Recognition**: An app recognizes objects using high-dimensional image embeddings. Quantization reduces the storage footprint and improves search speed, crucial for mobile devices with limited resources.

- **`diskANN`**: Utilizes the DiskANN algorithm for approximate nearest neighbor searches, optimized for speed and efficiency. Supports up to **4096 dimensions**.

  **When to Use:**

  - **Large-scale, high-dimensional data**: Best for big datasets where quick approximate searches are acceptable.
  - **Real-time applications**: When fast response times are critical.
  - **Scalability needs**: Suitable for applications expected to grow significantly.

  **Real-World Scenario:**

  - **Semantic Search Engines**: A search engine indexes millions of documents using embeddings from language models like BERT (768 dimensions). DiskANN allows users to get fast search results by efficiently handling high-dimensional data.

---

### **2. `datatype`**

Specifies the data type of the vector components.

#### **Options:**

- **`float32`** (default): 32-bit floating-point numbers.

  **When to Use:**

  - **High precision requirements**: Necessary when the application demands precise calculations.
  - **Standard ML embeddings**: Most machine learning models output float32 vectors.

  **Real-World Scenario:**

  - **Scientific Simulations**: In climate modeling, vectors represent complex data where precision is vital for accurate simulations and predictions.

- **`uint8`**: 8-bit unsigned integers.

  **When to Use:**

  - **Memory optimization**: Reduces storage needs when precision can be sacrificed.
  - **Quantized models**: When vectors are output from models that already quantize data.

  **Real-World Scenario:**

  - **Basic Image Features**: Storing color histograms for image retrieval systems, where each bin can be represented with an 8-bit integer.

- **`iln8`**: 8-bit integer with potentially specialized encoding (interpretation may vary; assuming it's an 8-bit integer with logarithmic encoding).

  **When to Use:**

  - **Custom quantization schemes**: When using specialized compression techniques that map floating-point values to an 8-bit integer scale.
  - **Edge devices**: Ideal for applications on devices with extreme memory limitations.

  **Real-World Scenario:**

  - **Audio Fingerprinting**: Compressing audio feature vectors for song recognition apps where storage and quick retrieval are essential.

---

### **3. `dimensions`**

The length of the vectors being indexed.

#### **Options:**

- **Range from 0 to 4096**, default is **1536**.

  **When to Consider Lower Dimensions (≤ 505):**

  - **Simpler models**: Applications using basic embeddings or feature vectors.
  - **Flat index type**: Required when using the `flat` index type due to its dimension limit.

  **Real-World Scenario:**

  - **Keyword Matching**: Using low-dimensional TF-IDF vectors for document similarity in a content management system.

  **When to Consider Higher Dimensions (506 - 4096):**

  - **Complex models**: Deep learning applications with high-dimensional embeddings.
  - **Advanced search features**: When richer representations of data are necessary for accuracy.

  **Real-World Scenario:**

  - **Face Recognition**: Using high-dimensional embeddings (e.g., 2048 dimensions) to represent facial features for security systems.

---

### **4. `distanceFunction`**

Determines how similarity between vectors is calculated.

#### **Options:**

- **`cosine`**: Measures the cosine of the angle between vectors.

  **When to Use:**

  - **Orientation-focused similarity**: When the magnitude is less important than the direction.
  - **Normalized data**: Ideal when vectors are normalized to unit length.

  **Real-World Scenario:**

  - **Document Similarity**: In text analytics, comparing documents based on topic similarity where word counts are normalized.

- **`dot product`**: Computes the scalar product of two vectors.

  **When to Use:**

  - **Magnitude matters**: When both direction and magnitude are significant.
  - **Machine learning models**: Often used in recommendation systems where strength of preferences is important.

  **Real-World Scenario:**

  - **Personalized Recommendations**: Matching users to products by calculating the dot product of user and item embeddings in a collaborative filtering system.

- **`euclidean`**: Calculates the straight-line distance between vectors.

  **When to Use:**

  - **Spatial distance relevance**: When physical distance correlates with similarity.
  - **High-dimensional data**: Suitable for embeddings where both magnitude and direction impact similarity.

  **Real-World Scenario:**

  - **Anomaly Detection**: Identifying outliers in network traffic patterns by measuring Euclidean distances in feature space.

---

### **Option Combinations and Their Preferred Use-Cases**

#### **Combination 1: Low-Dimensional, Exact Searches**

- **`vectorIndexType`**: `flat`
- **`datatype`**: `float32`
- **`dimensions`**: ≤ 505
- **`distanceFunction`**: `cosine`

**Real-World Scenario:**

- **Small-Scale Text Classification**: A startup builds a news categorization tool using word embeddings (300 dimensions). Exact cosine similarity searches ensure accurate article tagging without the overhead of approximate methods.

---

#### **Combination 2: High-Dimensional, Performance-Critical Applications**

- **`vectorIndexType`**: `diskANN`
- **`datatype`**: `float32`
- **`dimensions`**: 768 - 1536
- **`distanceFunction`**: `cosine` or `dot product`

**Real-World Scenario:**

- **Real-Time Recommendations**: A streaming service uses user and content embeddings (1024 dimensions) to provide instantaneous movie recommendations. DiskANN accelerates search times, offering a smooth user experience despite the large dataset.

---

#### **Combination 3: Storage-Efficient High-Dimensional Data**

- **`vectorIndexType`**: `quantizedFlat`
- **`datatype`**: `uint8` or `iln8`
- **`dimensions`**: 2048
- **`distanceFunction`**: `cosine`

**Real-World Scenario:**

- **Mobile Visual Search**: An app allows users to search for products by uploading photos. High-dimensional image embeddings are quantized to fit the storage constraints of mobile devices, and approximate searches provide quick results.

---

#### **Combination 4: Precision-Critical Scientific Computing**

- **`vectorIndexType`**: `flat`
- **`datatype`**: `float32`
- **`dimensions`**: 4096
- **`distanceFunction`**: `euclidean`

**Real-World Scenario:**

- **Genomic Data Analysis**: Researchers analyze genetic sequences represented as high-dimensional vectors. Precise Euclidean distance calculations are essential for identifying genetic similarities and mutations.

---

#### **Combination 5: Medium-Dimensional Data with Storage Constraints**

- **`vectorIndexType`**: `quantizedFlat`
- **`datatype`**: `uint8`
- **`dimensions`**: 500
- **`distanceFunction`**: `dot product`

**Real-World Scenario:**

- **IoT Sensor Data**: A network of sensors generates medium-dimensional vectors representing environmental data. Quantization reduces storage and transmission costs, and dot product calculations help in identifying patterns and anomalies efficiently.

---

### **Summary**

- **`flat` Index Type**: Use for low-dimensional, exact searches on smaller datasets.
- **`quantizedFlat` Index Type**: Choose when you need to balance performance and storage with acceptable accuracy loss in high-dimensional data.
- **`diskANN` Index Type**: Opt for large-scale, high-dimensional datasets where approximate searches suffice, and speed is critical.
- **`float32` Datatype**: Default choice for precision; use when storage is less of a concern.
- **`uint8` and `iln8` Datatypes**: Use for storage efficiency, particularly when data can be quantized.
- **Dimensions**: Match the dimensionality to your data and index type constraints.
- **Distance Functions**: Select based on the nature of similarity in your application—`cosine` for orientation, `dot product` when magnitude matters, and `euclidean` for spatial relevance.

By carefully selecting these options based on your application's specific needs, you can optimize Cosmos DB's vector search capabilities to achieve the desired balance between performance, accuracy, and resource utilization.

