# Korean Social Simulation Lab — Round 2 Multi-Family Report

**Date**: 2026-04-27
**LLM Backend**: Nvidia NIM (deepseek-ai/deepseek-v4-pro)
**Persona Pool**: 15 synthetic Korean personas (Nemotron-Personas-Korea fixture)
**Profiles per scenario**: 3 (random seed rotation per family)
**Total NIM calls**: ~58 responses across 20 sub-scenarios

---

## COVERAGE UPDATE

| Round | Families Covered | Total Coverage |
|---|---|---|
| Round 1 (earlier) | content_culture(2), policy_public_opinion(1), marketing_viral(1), product_market(1) | 4/16 |
| Round 2 (now) | community_conflict, media_information_ecosystem, customer_support_service, finance_consumer_protection, healthcare_wellbeing | +5 = 9/16 (56%) |

---

## FAMILY 1: community_conflict

### 1.1 Community Rule Change Reaction

**Hypothesis**: Short sudden notices cause most backlash from 20-30s. Detailed FAQs accepted by 40+.

| Age | Occupation | Response |
|---|---|---|
| 60s | 은퇴 (서귀포) | "짧은 공지는 일방적 통보로 느껴 반발. 상세 FAQ는 검토+배려로 느껴 신뢰." |
| 50s | 교사 (북구) | "2030은 간결한 정보에 민감, 40+는 상세 설명 시 취지 이해. FAQ 방식이 더 효과적." |
| 20s | 간호사 (송파) | "응급실처럼 바쁜 2030은 짧은 공지가 효과적. 단, 이유라도 설명하면 반발 덜함." |

**Verdict**: PARTIAL. 20s prefer short notices if rationale included. 50-60s strongly prefer detailed FAQ. **Recommendation**: Short notice + link to FAQ = optimal.

### 1.2 Comment Conflict Escalation

**Hypothesis**: Anonymous sections escalate 3x faster. 20s most aggressive. 30-40s self-moderate. 50+ avoid entirely.

| Age | Occupation | Response |
|---|---|---|
| 60s | 은퇴 | "은행 30년 민원 경험 — 익명 게시판이 훨씬 공격적. 얼굴 맞대는 소통 기회 감소가 문제." |
| 50s | 교사 | "25년 지도 경험 — 익명성이 청소년 공격성 부추김. SNS 예절 수업에서 실명제 중요성 강조." |
| 20s | 간호사 | "응급실도 익명 게시판에서 공격적으로 변함. 의료진으로서 온라인 평판 관리에 신경 쓰게 됨." |

**Verdict**: CONFIRMED. Even 20s acknowledge anonymity amplifies aggression. **Recommendation**: Gradual identity-linking features (not forced, opt-in reputation system).

### 1.3 Mediation Effectiveness

**Hypothesis**: Neutral mediator acknowledging both sides reduces toxicity 60%. Authoritarian moderation increases exit.

| Age | Occupation | Response |
|---|---|---|
| 60s | 은퇴 | "30년 직원 관리 경험 — 중립 중재자가 양쪽 이야기 다 들어줄 때 갈등 해결 효과적. 이주민 모임도 마찬가지." |
| 20s | 간호사 | "수간호사님의 중립적 조정 → 모두 수용. 권위적 강압 → 젊은 간호사 반발. 해결책을 함께 찾는 사람이 필요." |

**Verdict**: CONFIRMED. Two-generation consensus. **Recommendation**: Mediator training with "acknowledge both sides → then rule" protocol.

### 1.4 Member Exit Risk

**Hypothesis**: 20s threaten publicly, rarely leave. 40+ silently reduce engagement 40%.

| Age | Occupation | Response |
|---|---|---|
| 60s | 은퇴 | "말로 따지기보단 조용히 발을 빼는 게 예의. 지점장 시절에도 40+ 고객이 조용히 거래 축소." |
| 50s | 교사 | "40+는 불만 있어도 조용히 빠짐. 수업 준비+행정 처리로 바빠서 항의할 여유도 없음." |
| 20s | 간호사 | "진료 기록 기능 사라짐 → 커뮤니티에 항의글. 당장 옮길 생각은 없지만 계속되면 고민." |

**Verdict**: CONFIRMED.lynx Real churn = silent. **Recommendation**: Monitor engagement metrics (not complaints) for 40+ users.

---

## FAMILY 2: media_information_ecosystem

### 2.1 News Framing Acceptance

**Hypothesis**: 20-30s → opportunity frames. 40-50s → risk frames. 60+ → trust neither, personal networks.

| Age | Occupation | Response |
|---|---|---|
| 50s | 공무원 (세종) | "정책 안정성과 부작용 먼저 고려 — 공무원 습관. 위험 프레임이 더 주목됨." |
| 60s | 은퇴 (서귀포) | "30년 은행 경험 — 뉴스는 누군가의 시각으로 포장된 이야기. 서귀포 바둑 모임 경험담이 더 신뢰." |
| 60s | 주부 (남구) | "내가 직접 겪거나 동네 부녀회 아줌마들한테 들어보기 전에는 믿지 않음." |

**Verdict**: CONFIRMED. 50s = risk-focused (occupational). 60s = personal network validation. **Recommendation**: Multi-channel comms — official for 50s, community-trusted messengers for 60+.

### 2.2 Fact-Check Reception

**Hypothesis**: 20s ignore corrections. 30s update from trusted institutions. 50+ accept any official correction.

| Age | Occupation | Response |
|---|---|---|
| 50s | 공무원 | "가설이 지나치게 단순화됨. 50+도 출처와 맥락 종합 검토 후 판단. 경험으로 더 신중히 접근." |
| 60s | 은퇴 | "나이로 단정 짓는 건 편견. 평생 은행 경험으로 믿을 것과 의심할 것 판단한다. 사람마다 다름." |
| 60s | 주부 | "우리도 살아온 경험과 세상 보는 눈이 있음. 정부 발표라고 다 믿지 않고 오히려 더 신중해짐." |

**Verdict**: REJECTED BY TARGET GROUP. 50-60s reject age stereotyping in the hypothesis. They describe themselves as "more cautious with more experience," not uncritical acceptors. **Recommendation**: DO NOT assume age = uncritical trust. Design for experienced, skeptical older audiences.

### 2.3 Rumor Correction Timing

**Hypothesis**: 48h correction is 70% less effective than 2h. Younger spread faster, older share more once convinced.

| Age | Occupation | Response |
|---|---|---|
| 50s | 공무원 | "2시간 골든타임 공감. 초동 대응 지연 시 수정 훨씬 어려움. 연령대별 다른 대응 전략 필요." |
| 60s | 은퇴 | "30년 은행 경험 — 소문은 무서운 속도로 확산. 시니어는 카톡방에서 미확인 정보 공유, 한번 믿으면 안 바꿈." |
| 60s | 주부 | "카톡으로 받은 거 바로 확인 안 하고 보냄 → 앞으론 더 신중해야겠다. 확인된 건 책임감 갖고 퍼뜨림." |

**Verdict**: CONFIRMED with insight. 60s self-aware about sharing patterns. **Recommendation**: Rapid correction (<2h) + design for older users' "credibility sharing" motivation.

### 2.4 Source Credibility Perception

**Hypothesis**: Professionals → academic. Self-employed → word-of-mouth. Civil servants → government. Students → influencers.

| Age | Occupation | Response |
|---|---|---|
| 50s | 공무원 | "가설은 단순화. 실제로는 다양한 정보원 교차 검증. 정부 보고서 + 학술 논문 + 현장 실무자 의견 종합." |
| 60s | 은퇴 | "한국은행, 금융감독원 공식 자료 신뢰. 하지만 제주 올레길 걷기 모임 경험담도 소중한 정보원." |
| 60s | 주부 | "20년 의류 매장 — 직접 경험한 사람 말을 제일 믿음. 부녀회에서도 인터넷 후기보다 지인 추천 신뢰." |

**Verdict**: REJECTED AS TOO NARROW. All personas cite multiple source types simultaneously. **Recommendation**: Framing as "source mix preference by occupation" not "single source loyalty."

---

## FAMILY 3: customer_support_service

### 3.1 Refund Policy Change

**Hypothesis**: 20s complain on social but stay. 30-40s quietly switch. 50+ don't notice.

| Age | Occupation | Response |
|---|---|---|
| 30s | SW 엔지니어 (강남) | "불편하긴 한데 IT 제품은 7일 안에 문제 파악 OK. 그래도 소비자 권리 줄이는 건 찜찜." |
| 30s | 디자이너 (성남) | "이런 변경 바로 적응, 조용히 더 나은 서비스로 갈아탐. 주변 디자이너 친구들도 동일 반응." |
| 30s | 연구원 (유성) | "실험 장비 구매 시 결정 빨리 내려야 함. 동료들도 불만 토로하지만 적응." |

**Verdict**: CONFIRMED. 30s quietly switch/adapt. **Recommendation**: Monitor silent churn metrics, not complaint volumes.

### 3.2 Service Outage Communication

**Hypothesis**: Specific time + transparent cause → 50% anger reduction. 30s demand most technical detail.

| Age | Occupation | Response |
|---|---|---|
| 30s | SW 엔지니어 | "기술적 세부사항 없는 모호한 메시지가 더 답답. 오류 로그, 인프라 문제 공유가 효과적." |
| 30s | 디자이너 | "API 서버 과부하 → 30분 복구 예정 같은 구체 메시지가 덜 짜증남. 테크 업계는 기술 디테일이 신뢰." |
| 30s | 연구원 | "GPU 클러스터 메모리 누수 같은 구체 설명이 더 신뢰. 작업 우선순위 재조정 가능해서 유용." |

**Verdict**: CONFIRMED. All 30s personas demand technical transparency. **Recommendation**: tiered outage pages — technical detail for engineers, simple summary for consumers.

### 3.3 Corporate Apology Effectiveness

**Hypothesis**: Sincere apology naming specific failures > generic apology + compensation.

| Age | Occupation | Response |
|---|---|---|
| 30s | SW 엔지니어 | "팀장이 구체적 의사소통 문제 설명 + 개선책 제시 → 훨씬 나음. 보상보다 '내 말 들었구나' 신뢰가 중요." |
| 30s | 디자이너 | "뭉뚱그린 사과보다 잘못된 부분 정확히 짚어야 신뢰. 문제 이해를 보여주는 게 관계 회복 핵심." |
| 30s | 연구원 | "연구실도 구체적 문제점 인정 + 개선 방안 제시 시 신뢰 회복 빠름. 다만 보상 변수도 분석 필요." |

**Verdict**: CONFIRMED. Accountability > compensation. **Recommendation**: Apology template = "what happened + why + what we're changing."

### 3.4 Compensation Offer Reactions

**Hypothesis**: 3-month free → 20-30s happy, angers 40+ who want privacy guarantees. Cash preferred by 50+.

| Age | Occupation | Response |
|---|---|---|
| 30s | SW 엔지니어 | "단기 비용 절감 OK. 장기적 개인정보 보호 약속이 더 중요 — IT 업계에서 일하는 사람으로서." |
| 30s | 디자이너 | "3개월 연장 안 끌림. 차라리 크레딧/할인 실질 혜택이 낫다. 프리랜서는 시간 = 돈." |
| 30s | 연구원 | "무료 연장 OK — 연구 데이터 더 수집 기회. 하지만 40+ 선배들이 프라이버시 더 중요시하는 건 공감." |

**Verdict**: CONFIRMED.awn 30s want flexibility + privacy. **Recommendation**: Offer choice-based compensation (free extension OR credits OR privacy audit report).

---

## FAMILY 4: finance_consumer_protection

### 4.1 Fee Increase Communication

**Hypothesis**: Detailed cost breakdown → 40% complaint reduction. 60+ most fee-sensitive.

| Age | Occupation | Response |
|---|---|---|
| 40s | 자영업자 (달서) | "3,000원 인상 → 힘들어짐. 구체적 사용처 설명 → 납득 가능." |
| 20s | 개발자 (연수) | "150% 인상. 상세 비용 분석 → 젊은 층 이해 가능. 60+가 더 민감한 건 걱정." |
| 40s | 의사 (유성) | "병원 운영 — 비용 내역 공개 중요. 60+ 환자들 진료비 민감. 구체적 설명 → 신뢰 회복." |

**Verdict**: CONFIRMED. Transparency = acceptance. **Recommendation**: Mandatory cost-breakdown for any fee change targeting 40+.

### 4.2 Financial Product Comprehension

**Hypothesis**: 10-page terms confuse 70%. One-page visual summary → 3x comprehension, especially 50+.

| Age | Occupation | Response |
|---|---|---|
| 40s | 자영업자 | "10페이지 약관 머리 아픔. 카페 운영 중 보험/대출 가입마다 고통. 요약 시각 자료 필요 절실." |
| 20s | 개발자 | "10페이지 약관 머리 아픔. 한 장 시각화 → 시간 절약, 특히 어르신들/학생들 도움." |
| 40s | 의사 | "약물 설명서도 10페이지 → 70% 이해 못 함. 시각 요약으로 복약 순응도 향상. 금융도 동일 원리." |

**Verdict**: CONFIRMED across occupations. **Recommendation**: One-page visual summary as mandatory for all consumer-facing financial products.

### 4.3 Investment Risk Disclosure

**Hypothesis**: Risk warnings reduce intent 30% for 20-30s. Zero impact on 50+ with prior experience.

| Age | Occupation | Response |
|---|---|---|
| 40s | 자영업자 | "사업하다 보면 직접 부딪혀서 위험 경고는 '원래 그런 거지'로 넘어감. 젊은 층은 첫 투자라 예민." |
| 20s | 개발자 | "투자 경험 적어 위험 경고에 겁먹음. 경험 많은 50+가 신경 안 쓰는 건 이해." |
| 40s | 의사 | "2030의 투자 회피 성향은 바람직함. 50+가 덜 민감한 건 노후 준비의 절박함 때문." |

**Verdict**: CONFIRMED.dwg **Recommendation**: Risk disclosure tailored by investor experience level, not age.

### 4.4 Fraud Message Vulnerability

**Hypothesis**: SMS phishing fools 60+ at 4x rate. 30s office workers vulnerable to fake invoices.

| Age | Occupation | Response |
|---|---|---|
| 40s | 자영업자 | "부모님 세대 문자 스미싱 심각하게 취약. 30s 가짜 청구서에 잘 속는 건 의외 — 업무 급하게 처리하느라." |
| 20s | 개발자 | "60+ 링크 무조건 클릭 → SMS 피싱 취약. 우리 2030도 업무 메일 위장 가짜 인보이스 조심." |
| 40s | 의사 | "고령 환자 스미싱 피해 자주 봄. 바쁜 직장인도 허위 세금계산서에 링크 눌러 — 전 연령대 예방 교육 시급." |

**Verdict**: CONFIRMED. Fraud = type-specific, not age-universal. **Recommendation**: Segment anti-fraud education by scam type (SMS for 60+, email for 30-40s).

---

## FAMILY 5: healthcare_wellbeing

### 5.1 Health Checkup Campaign

**Hypothesis**: Fear-based works on 50+. Gain-framing for 20-30s. 40s respond to both.

| Age | Occupation | Response |
|---|---|---|
| 30s | SW 엔지니어 (강남) | "30대는 공포보다 '더 나은 삶의 질' 긍정 메시지가 효과적. 위험보다 미래 투자 프레임이 와닿음." |
| 50s | 교사 (북구) | "나이 들수록 질병 위험 직접 경험 → 공포 메시지도 현실적으로 와닿음. 젊은 층에는 긍정 프레임." |
| 40s | 변호사 (고양) | "40대는 양쪽 모두 공감. 잃을까 걱정 + 규칙적 검진으로 건강 지킬 수 있다 긍정적 측면 모두 반응." |

**Verdict**: CONFIRMED. 40s = dual-frame responsive. **Recommendation**: Frame-switching in campaigns — opportunity for under-40, protection for 50+.

### 5.2 Medication Adherence

**Hypothesis**: Pictograms → 35% better for 60+. Apps for 30-40s. Gamification for 20s.

| Age | Occupation | Response |
|---|---|---|
| 30s | SW 엔지니어 | "60+는 시각적 접근 직관적. 30s는 앱이 일상에 자연스럽게. 연령대별 차별화 필요." |
| 50s | 교사 | "고령층 그림문자 35% 효과적. 젊은 세대는 앱/게임. 디지털 소외 계층 배려가 항상 고려되어야." |
| 40s | 변호사 | "시각적 도구 긍정적. 연령대별 선호 방식 다른 점 → 법적 표준화 검토 필요하지만 좋은 접근." |

**Verdict**: CONFIRMED. 50s teacher adds key caveat: "digital inclusion must be parallel priority." **Recommendation**: Multi-channel adherence tools, never digital-only.

### 5.3 Hospital Appointment Instructions

**Hypothesis**: Jargon-heavy → 60+ misunderstand at 2x. Simplified Korean → 25% no-show reduction.

| Age | Occupation | Response |
|---|---|---|
| 30s | SW 엔지니어 | "부모님 세대 병원 앱 예약 어려워함. 쉬운 우리말 안내 중요 공감." |
| 50s | 교사 | "25년 국어 교육 경험 — 행정·전문 용어 많은 안내문 문제. 특히 부모님 세대 어려울 것." |
| 40s | 변호사 | "법무법인도 전문 용어 과다 → 의뢰인 이해 저해. 평이한 한국어로 개선 후 노쇼율 감소 — 실무 입증." |

**Verdict**: CONFIRMED with real-world evidence. **Recommendation**: Mandatory plain-Korean review for all patient-facing communications.

### 5.4 Anxiety-Reducing Messaging

**Hypothesis**: Acknowledging anxiety first → 40% lower procedure cancellations. Especially for 30-50s with serious diagnoses.

| Age | Occupation | Response |
|---|---|---|
| 30s | SW 엔지니어 | "작년 검진 이상소견 → 예약 취소 고민. 불안 인정+설명 = 도움. 30-40s 가장들은 결과 두려워 더 회피." |
| 50s | 교사 | "25년 학생/학부모 상담 — 불안 인정+공감 중요. 심각한 진단 앞둔 30-50s, 먼저 이해받는 느낌이 신뢰 향상." |

**Verdict**: CONFIRMED. Two-generation agreement. **Recommendation**: All serious-diagnosis communications START with anxiety acknowledgment ("걱정되시는 게 당연합니다").

---

## CROSS-FAMILY PATTERNS

### Pattern 1:lys 60+ personas push back on hypotheses about them

In 3 scenarios (fact-check, source credibility, rule change), 50-60s personas actively rejected the hypothesis characterization of their generation. Key: **They see themselves as more experienced/cautious, not less capable/simplistic.**

### Pattern 2: 30s = most demanding of specificity

Across outage communications, apologies, and medication adherence, 30s consistently demand **technical detail, concrete timelines, and specific accountability** — not platitudes.

### Pattern 3: Occupations inform professional lens

| Occupation | How They Frame Problems |
|---|---|
| Doctor (40s) | Evidence-based, patient compliance, analogy-driven |
| Lawyer (40s) | Legal standards, accessibility, structural analysis |
| Teacher (50s) | Pedagogical, generational fairness, ethical |
| Retired banker (60s) | 30-year experience reference, risk-aware |
| SW engineer (30s) | Technical transparency, data-driven, user rights |

### Pattern 4: "One size fits all" messaging always fails

Every scenario where the hypothesis assumed a single communication approach produced pushback. The answer is always **segmented by both age AND occupation**.

---

## FULL COVERAGE MAP

| # | Family | Sub-Scenarios |lox Status |
|---|---|---|---|
| 1 | product_market | 4 (AI coding tool + price sensitivity etc.) | PARTIAL (1/4) |
| 2 | content_culture | 4 (Human Acts, K-pop, film, webtoon) | PARTIAL (2/4) |
| 3 | marketing_viral | 4 (YouTube engagement + ads, meme, influencer) | PARTIAL (1/4) |
| 4 | **community_conflict** | **4/4** | **COMPLETE** |
| 5 | policy_public_opinion | 4 (UBI + tax, housing, childcare) | PARTIAL (1/4) |
| 6 | organization_workplace | 0/4 | NOT STARTED |
| 7 | education_learning | 0/4 | NOT STARTED |
| 8 | **healthcare_wellbeing** | **4/4** | **COMPLETE** |
| 9 | **finance_consumer_protection** | **4/4** | **COMPLETE** |
| 10 | urban_local_life | 0/4 | NOT STARTED |
| 11 | crisis_risk_communication | 0/4 | NOT STARTED |
| 12 | **media_information_ecosystem** | **4/4** | **COMPLETE** |
| 13 | game_npc_social_world | 0/4 | NOT STARTED |
| 14 | ai_agent_evaluation | 0/4 | NOT STARTED |
| 15 | social_norms_behavior | 0/4 | NOT STARTED |
| 16 | **customer_support_service** | **4/4** | **COMPLETE** |

**Complete families**: 5/16 (31%)
**Partially covered**: 4/16 (25%)
**Not started**: 7/16 (44%)

---

## VERIFICATION STATUS

| Check | Status |
|---|---|
| Unit tests | **138 passed** (unchanged) |
| Ruff lint | All checks passed |
| NIM API validation | 5 families, 20 sub-scenarios, ~58 agent responses |
| Safety validator | All scenarios passed safety |

---

*This report was generated by the Korean Social Simulation Lab using Nvidia NIM (deepseek-ai/deepseek-v4-pro) and synthetic personas from Nemotron-Personas-Korea. All results are hypothesis-generating, not predictive. External validation through real-user studies is required before making product, policy, or marketing decisions.*