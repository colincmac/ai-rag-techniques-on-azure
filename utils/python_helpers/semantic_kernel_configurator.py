from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
service_id = "default"

base_kernel = Kernel()

base_kernel.add_service(
  AzureChatCompletion(
    service_id=service_id
  )
)