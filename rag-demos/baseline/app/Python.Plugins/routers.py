from fastapi import APIRouter
from pydantic import BaseModel

router = APIRouter()

@router.post("/classify")
def classify_text() -> str:
    return ""

@router.get("/test")
def classify_text() -> str:
    return "hello world"