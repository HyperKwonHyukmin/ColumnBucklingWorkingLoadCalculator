using System;
using ColumnBucklingApp.Models;
using ColumnBucklingApp.Services;

namespace ColumnBucklingApp
{
  /// <summary>
  /// 개발자가 코드에서 직접 부재를 선택할 수 있도록 미리 정의해 둔 상수 구조체입니다.
  /// 오타를 방지하고 자동완성(IntelliSense)을 지원하기 위해 사용합니다.
  /// </summary>
  public struct AvailableMembers
  {
    public const string H200x200 = "H200x200";
    public const string H200x204 = "H200x204";
    public const string H295x302 = "H295x302";
    public const string H300x300 = "H300x300";
    public const string H300x305 = "H300x305";

    public const string Pipe300A = "300A PIPE";
    public const string Pipe250A_60 = "250A PIPE (#60)";
    public const string Pipe400A_40 = "400A PIPE (#40)";
    public const string Pipe400A_60 = "400A PIPE (#60)";

    public const string IA150 = "150 I.A (150x90x9/12)";
    public const string IA200 = "200 I.A (200x90x9/14)";
    public const string IA250 = "250 I.A (250x90x10/15)";

    public const string I400 = "I400x150x12.5x25";
    public const string I350_24 = "I350x150x12x24";
    public const string I350_15 = "I350x150x9x15";
  }

  class Program
  {
    /// <summary>
    /// 애플리케이션의 진입점(Entry Point)입니다.
    /// 외부 텍스트 파일(부재치수2.txt)에서 부재 제원 데이터를 로드하고,
    /// 코드에서 선택된 구조체 상수를 바탕으로 좌굴 허용 하중을 계산합니다.
    /// </summary>
    static void Main(string[] args)
    {
      Console.WriteLine("===============================================");
      Console.WriteLine("   기둥 좌굴 허용 하중 계산 프로그램 (AISC)   ");
      Console.WriteLine("===============================================\n");

      // 1. 데이터베이스 서비스 초기화 및 부재 데이터 로드
      var dbService = new MemberDatabaseService();
      string dataFilePath = @"C:\Coding\Csharp\Projects\ColumnBucklingWorkingLoadCalculator\Reference\PropertyRefer.txt";

      try
      {
        Console.WriteLine($"[시스템] '{dataFilePath}' 파일에서 데이터를 불러오는 중...");
        dbService.LoadFromTextFile(dataFilePath);
        Console.WriteLine("[시스템] 데이터 로드 완료!\n");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[오류] 데이터 로드 실패: {ex.Message}");
        Console.ReadLine();
        return;
      }

      // =========================================================================
      // [개발자 수정 영역]
      // 2. 부재 선택 (AvailableMembers 구조체에서 원하는 부재를 선택하세요)
      // 예시: AvailableMembers.Pipe300A, AvailableMembers.H295x302 등
      // =========================================================================
      string targetName = AvailableMembers.H300x305;


      MemberProfile selectedMember;
      try
      {
        // 선택된 부재 이름으로 데이터베스 검색
        selectedMember = dbService.GetProfile(targetName);
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[오류] {ex.Message}");
        Console.ResetColor();
        Console.ReadLine();
        return;
      }

      // 3. 좌굴 계산 입력값 세팅
      var input = new BucklingInput
      {
        Member = selectedMember,     // 선택한 부재 객체 주입
        SafetyFactor = 3.0,          // 사용하중 안전율
        ElasticModulus = 210000,     // 탄성계수 E [MPa]
        YieldStress = 240,           // 항복응력 Fy [MPa]
        Length = 4470,               // 기둥 길이 L [mm]
        EccentricityRatio = 0.25     // 편심 기준 입력
      };

      // 4. 비즈니스 로직 실행
      var calculatorService = new BucklingCalculatorService();

      try
      {
        double workingLoad = calculatorService.CalculateWorkingLoad(input);

        // 5. 결과 출력
        Console.WriteLine("-----------------------------------------------");
        Console.WriteLine("[입력 정보 요약]");
        Console.WriteLine($"- 선택된 부재   : {input.Member.Name}");
        Console.WriteLine($"- 단면적(A)     : {input.Member.Area:F2} mm^2");
        Console.WriteLine($"- 관성모멘트(I) : {input.Member.MomentOfInertia:F2} mm^4");
        Console.WriteLine($"- 기둥 길이(L)  : {input.Length} mm");
        Console.WriteLine($"- 편심 비율     : {input.EccentricityRatio}");
        Console.WriteLine("-----------------------------------------------");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[최종 계산 결과]");
        Console.WriteLine($"=> 최대 허용 사용하중 : {workingLoad:F3} ton\n");
        Console.ResetColor();
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[오류] 계산 중 문제가 발생했습니다: {ex.Message}");
        Console.ResetColor();
      }
    }
  }
}
