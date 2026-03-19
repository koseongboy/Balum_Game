import org.jsoup.Jsoup;
import org.jsoup.nodes.Document;
import org.jsoup.nodes.Element;
import org.jsoup.select.Elements;
import java.util.Arrays;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.io.*;
import java.nio.charset.StandardCharsets;

interface Crawler {
    String getTheme();
    List<String> fetch(int limit);
}

class NewsCrawler implements Crawler {
    @Override
    public String getTheme() { return "News"; }

    private static final List<String> PRESIDENT_NAMES = Arrays.asList(
        "이승만", "윤보선", "박정희", "최규하", "전두환", "노태우", 
        "김영삼", "김대중", "노무현", "이명박", "박근혜", "문재인", "윤석열"
    );

    @Override
    public List<String> fetch(int limit) {
        List<String> results = new ArrayList<>();
        try {
            // 네이버 뉴스 속보(전체) 페이지 URL
            // 최신 뉴스 + 매번 바뀌게 하기 위해 속보로 설정
            String listUrl = "https://news.naver.com/main/list.naver?mode=LSD&mid=sec&sid1=001";
            
            // .userAgent: 네이버 봇 차단 회피
            // 봇 차단을 회피하기 위해 브라우저에서 접속하는 척 설정
            Document listDoc = Jsoup.connect(listUrl)
                    .userAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
                    .get();

            // 기사 목록에서 상세 기사로 가는 링크(a 태그)들을 선택
            // ul.type06_headline 및 ul.type06 내부에 있는 제목 링크만 골라냄 (:not(.photo)는 이미지 링크 제외)
            Elements links = listDoc.select("ul.type06_headline li dt:not(.photo) a, ul.type06 li dt:not(.photo) a");
            
            List<String> urlList = new ArrayList<>();
            for (Element link : links) {
                String href = link.absUrl("href"); // 상대 경로를 절대 경로(https://...)로 변환
                if (!href.isEmpty()) urlList.add(href);
            }

            // 수집한 기사 순서를 랜덤하게 섞음 (매번 다른 문장을 얻기 위함)
            Collections.shuffle(urlList);

            // 개별 기사 페이지에 접속하여 본문 내용 추출
            for (String detailUrl : urlList) {
                // 목표 수량(limit)을 채우면 반복 중단
                if (results.size() >= limit) break;

                try {
                    // 기사 상세 페이지 접속
                    Document detailDoc = Jsoup.connect(detailUrl)
                            .userAgent("Mozilla/5.0")
                            .timeout(5000) // 5초 안에 응답 없으면 포기
                            .get();

                    // 네이버 뉴스 본문이 들어있는 특정 ID(#dic_area) 선택
                    Element contentElement = detailDoc.selectFirst("#dic_area");
                    
                    if (contentElement != null) {
                        // 본문 내 불필요한 요소(이미지 설명, 기자 정보, 스크립트 등) 제거
                        contentElement.select(".img_desc, .reporter_area, script, iframe, span.end_photo_org").remove();
                        
                        String fullText = contentElement.text().trim();
                        
                        // 문장 단위로 분리 (정규표현식)
                        String[] sentences = fullText.split("(?<=[.!?])\\s+");

                        for (String s : sentences) {
                            s = s.trim();
                            
                            // 영어 포함 여부 체크 (정규식: 알파벳 A-Z, a-z가 포함되면 true)
                            boolean hasEnglish = s.matches(".*[a-zA-Z].*");

                            boolean hasPresidentName = false;
                            for (String name : PRESIDENT_NAMES) {
                                if (s.contains(name)) {
                                    hasPresidentName = true;
                                    break;
                                }
                            }
                            
                            // 문장 길이가 25자 이상 70자 이하 (너무 짧거나 길면 게임에 부적합)
                            // "재배포 금지" 같은 상투적인 문구가 없어야 함
                            // 영어가 포함되지 않은 순수 한글 문장
                            if (s.length() >= 25 && s.length() <= 70 && 
                                !s.contains("재배포 금지") && !hasEnglish) {
                                
                                if (results.size() < limit) {
                                    results.add(s);
                                }
                            }
                        }
                    }
                    // 서버 과부하 방지 및 IP 차단 회피를 위해 0.2초간 대기
                    Thread.sleep(200); 
                    
                } catch (Exception e) {
                    System.err.println("상세 페이지 접속 실패: " + detailUrl);
                }
            }
        } catch (Exception e) {
            System.err.println("뉴스 목록 로딩 실패: " + e.getMessage());
        }
        return results;
    }
}

// 2. 잰말놀이 크롤러 (위키백과)
class TongueTwisterCrawler implements Crawler {
    @Override
    public String getTheme() { return "TongueTwister"; }

    @Override
    public List<String> fetch(int limit) {
        List<String> results = new ArrayList<>();
        try {
            Document doc = Jsoup.connect("https://ko.wikipedia.org/wiki/%EC%9E%B0%EB%A7%90%EB%86%80%EC%9D%B4")
                                .userAgent("Mozilla/5.0")
                                .get();

            // "한국어" 섹션의 ID를 가진 span이나 element 찾음
            Element startNode = doc.getElementById("한국어");
            if (startNode == null) {
                // ID가 span에 들어있는 경우를 대비해 하나 더 체크
                startNode = doc.selectFirst("h3:contains(한국어)");
            }

            if (startNode != null) {
                // 한국어 섹션의 부모(h3)로부터 아래로 내려가며 탐색
                Element current = startNode.parent(); 
                
                while (current != null) {
                    current = current.nextElementSibling();
                    
                    // 다음 큰 섹션(영어 등 다른 언어)이 나오면 중단
                    if (current != null && (current.tagName().equals("h2") || current.tagName().equals("h3"))) {
                        // 만약 다음 제목에 "한국어"가 포함되어 있지 않다면 끝내기
                        if (!current.text().contains("한국어")) break;
                    }

                    // 리스트(ul)를 발견하면 그 안의 li들을 전부 긁음
                    if (current != null && current.tagName().equals("ul")) {
                        Elements liTags = current.select("li");
                        for (Element li : liTags) {
                            String rawText = li.text();
                            
                            // 필터링: 주석[1], 발음 설명( ) 등 제거
                            String cleaned = rawText.split("\\[")[0].split("\\(")[0].trim();

                            // 한글이 포함된 실제 문장만 추가
                            if (cleaned.matches(".*[가-힣].*") && cleaned.length() > 5) {
                                results.add(cleaned);
                            }
                        }
                    }
                }
            }
        } catch (Exception e) {
            System.err.println("TongueTwister 크롤링 에러: " + e.getMessage());
        }
        return results;
    }
}

// 메인 실행 클래스
public class SentenceCrawler {
    public static void main(String[] args) {
        String filePath = "../SentenceData.tsv"; 
        List<String[]> allData = new ArrayList<>();

        // 크롤러 리스트 생성
        List<Crawler> crawlers = new ArrayList<>();
        crawlers.add(new NewsCrawler());
        crawlers.add(new TongueTwisterCrawler());

        System.out.println("=== 데이터 수집 시작 ===");

        for (Crawler crawler : crawlers) {
            System.out.println("[" + crawler.getTheme() + "] 테마 수집 중...");
            List<String> sentences = crawler.fetch(15); // 각 테마별 15개씩
            for (String s : sentences) {
                allData.add(new String[]{crawler.getTheme(), "1", s});
            }
        }

        saveToTsvForUnity(allData, filePath);
    }

    private static void saveToTsvForUnity(List<String[]> data, String path) {
    File file = new File(path);
    
        // 폴더가 없으면 생성
        if (file.getParentFile() != null) {
            file.getParentFile().mkdirs();
        }

        // UTF-8 with BOM 설정
        try (BufferedWriter writer = new BufferedWriter(
                new OutputStreamWriter(new FileOutputStream(file), StandardCharsets.UTF_8))) {
            
            // [수정사항] 파일의 가장 처음에 BOM(Byte Order Mark) 삽입
            // 이 코드가 있어야 유니티 인스펙터나 런타임에서 한글이 깨지지 않음
            writer.write("\uFEFF"); 

            // 헤더 작성 (대문자로 시작하는 Attribute 이름들)
            writer.write("Id\tTheme\tDifficulty\tSentence");
            writer.newLine();

            int id = 1;
            for (String[] row : data) {
                // 문장 내 탭(\t)이나 줄바꿈(\n, \r)이 있으면 TSV 구조가 망가지므로 공백으로 치환
                String cleanSentence = row[2].replace("\t", " ")
                                            .replace("\n", " ")
                                            .replace("\r", " ");
                
                // 데이터 작성 (탭으로 구분)
                writer.write(id++ + "\t");      // Id
                writer.write(row[0] + "\t");    // Theme
                writer.write(row[1] + "\t");    // Difficulty (나중에 수정할 수 있게 기본값 1 저장됨)
                writer.write(cleanSentence);    // Sentence
                writer.newLine();
            }

            System.out.println("=== 작업 완료 (UTF-8 BOM 적용됨) ===");
            System.out.println("저장 경로: " + file.getAbsolutePath());

        } catch (Exception e) {
            System.err.println("파일 저장 중 오류 발생: " + e.getMessage());
            e.printStackTrace();
        }
    }
}