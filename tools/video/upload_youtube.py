"""Đăng một bài học lên YouTube. Token nằm ngoài repo."""

from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path
from typing import Optional

from google.auth.transport.requests import Request
from google.oauth2.credentials import Credentials
from google_auth_oauthlib.flow import InstalledAppFlow
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

# youtube (không chỉ youtube.upload) để vừa tải lên vừa chuyển video sang công khai.
SCOPES = ["https://www.googleapis.com/auth/youtube"]
REPO = Path(__file__).resolve().parents[2]


def auth_dir() -> Path:
    folder = Path(os.environ["APPDATA"]) / "iSurvey" / "youtube"
    folder.mkdir(parents=True, exist_ok=True)
    return folder


def get_creds() -> Credentials:
    folder = auth_dir()
    token_path = folder / "token.json"
    secret_path = folder / "client_secret.json"
    creds = None
    if token_path.exists():
        creds = Credentials.from_authorized_user_file(str(token_path), SCOPES)
    if creds is None or not creds.valid:
        if creds is not None and creds.expired and creds.refresh_token:
            creds.refresh(Request())
        else:
            if not secret_path.exists():
                sys.exit(
                    "Chưa có client_secret.json.\n"
                    f"Đặt file tại: {secret_path}\n"
                    "Các bước nằm trong docs/video/YOUTUBE.md"
                )
            flow = InstalledAppFlow.from_client_secrets_file(str(secret_path), SCOPES)
            creds = flow.run_local_server(port=0, open_browser=True)
        token_path.write_text(creds.to_json(), encoding="utf-8")
    return creds


def lesson_video(lesson: dict) -> Path:
    folder = REPO / "docs" / "video" / "thanh-pham" / lesson["id"]
    video = folder / f"{lesson['id']}.mp4"
    if not video.exists():
        sys.exit(f"Chưa thấy {video}. Chạy tools/video/mux.ps1 trước.")
    return video


def upload(lesson_path: Path, privacy: Optional[str]) -> None:
    lesson = json.loads(lesson_path.read_text(encoding="utf-8"))
    video = lesson_video(lesson)
    status = privacy or lesson.get("privacy", "unlisted")
    youtube = build("youtube", "v3", credentials=get_creds())
    body = {
        "snippet": {
            "title": lesson["title"],
            "description": lesson["description"],
            "tags": lesson.get("tags", []),
            "categoryId": "27",
            "defaultLanguage": "vi",
            "defaultAudioLanguage": "vi",
        },
        "status": {
            "privacyStatus": status,
            "selfDeclaredMadeForKids": False,
        },
    }
    media = MediaFileUpload(str(video), chunksize=8 * 1024 * 1024, resumable=True)
    request = youtube.videos().insert(part="snippet,status", body=body, media_body=media)
    response = None
    while response is None:
        chunk, response = request.next_chunk()
        if chunk is not None:
            print(f"uploaded {int(chunk.progress() * 100)}%", flush=True)
    video_id = response["id"]
    print(f"https://youtu.be/{video_id}")
    print(f"privacy={status}")
    print(f"file={video}")


def set_public(video_id: str) -> None:
    youtube = build("youtube", "v3", credentials=get_creds())
    youtube.videos().update(
        part="status",
        body={
            "id": video_id,
            "status": {
                "privacyStatus": "public",
                "selfDeclaredMadeForKids": False,
            },
        },
    ).execute()
    print(f"https://youtu.be/{video_id}")
    print("privacy=public")


def login() -> None:
    get_creds()
    print(f"token={auth_dir() / 'token.json'}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--login", action="store_true")
    parser.add_argument("--lesson")
    parser.add_argument("--privacy", choices=("public", "unlisted", "private"))
    parser.add_argument("--set-public")
    args = parser.parse_args()
    if args.login:
        login()
        return
    if args.set_public:
        set_public(args.set_public)
        return
    if not args.lesson:
        sys.exit("Cần --lesson, --login, hoặc --set-public.")
    upload(Path(args.lesson), args.privacy)


if __name__ == "__main__":
    main()
