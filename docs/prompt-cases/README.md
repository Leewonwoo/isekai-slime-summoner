# 프롬프트 개선 사례 보관소

기술 문서 PDF의 "개선 사례 2~3개 (before/after)" 재료. **실패→개선을 겪은 그 날 바로 기록한다.**

## 기록 방법

1. 사례마다 폴더 하나: `case-01-unit-style/`, `case-02-wave-csv/` …
2. 폴더 안에 `case.md` (아래 양식) + 결과물 스크린샷/이미지 (`v1.png`, `v2.png` …)
3. 에셋 관련 사례면 [asset-ledger.csv](../asset-ledger.csv)의 해당 행과 파일명으로 연결

## case.md 양식

```markdown
# 사례 N: (한 줄 제목)

- 날짜: 2026-07-XX
- 도구: (ChatGPT 이미지 / Claude Code / Suno / …)
- 목표: 무엇을 만들려 했나

## v1 — 실패
- 프롬프트: (전문)
- 결과: v1.png / (코드면 요약)
- 문제점: 무엇이 왜 안 됐나

## v2 — 개선
- 바꾼 것: (프롬프트에서 무엇을 어떻게 수정했나)
- 사용 기법: (예: negative prompt, few-shot 예시 제공, 스타일 앵커 고정, 역할 지정, JSON 스키마 강제 …)
- 프롬프트: (전문)
- 결과: v2.png

## 교훈
- (다음에 재사용할 수 있는 한 줄 원칙)
```

> **사용 기법 이름을 반드시 명명할 것** — 심사 문서에 "few-shot", "negative prompt" 같은 기법명 표기가 요구됨 (SPEC §7-4).
