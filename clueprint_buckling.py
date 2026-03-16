from main import *
from flask import Blueprint, render_template, request, session, flash
import os
import subprocess
import re

# 프로젝트 폴더 설정 (기존 blueprint_beam.py와 동일하게 세팅)
baseDirectory = r'C:\Users\HHI\KHM\HiTessCloud_Flask'

# Buckling Analysis 페이지에 사용할 Blueprint 생성
blueprint = Blueprint('buckling', __name__, url_prefix='/buckling')


## 기둥 좌굴 계산 라우터 ######################################################################################
@blueprint.route('/columnbuckling', methods=['GET', 'POST'])
@login_required
def buckling_calculate():
    # C# 실행 파일(.exe)이 있는 경로 설정 (실제 경로에 맞게 수정 필요)
    programDirectory = os.path.join(baseDirectory, r"main\EngineeringPrograms\Buckling")
    exe_path = os.path.join(programDirectory, "ColumnBucklingApp.exe")

    if request.method == 'POST':
        # 1. 프론트엔드(HTML)에서 전송한 폼 데이터(Input) 받기
        safety_factor = request.form.get('safetyFactor', '3.0')
        member_select = request.form.get('memberSelect')
        material = request.form.get('material')
        length = request.form.get('length')
        eccentricity = request.form.get('eccentricity')

        try:
            # 2. subprocess로 C# 콘솔 프로그램 호출
            result = subprocess.run(
                [exe_path, member_select, length, eccentricity],
                capture_output=True,
                text=True,
                encoding='utf-8'  # 한글 출력이 깨질 경우 'cp949'로 변경
            )

            output = result.stdout

            # 3. 정규 표현식(Regex)을 사용하여 C# 출력 텍스트에서 결과값 추출
            area_match = re.search(r'- 단면적\(A\)\s*:\s*([0-9\.]+)', output)
            inertia_match = re.search(r'- 관성모멘트\(I\)\s*:\s*([0-9\.]+)', output)
            radius_match = re.search(r'- 회전반경\(r\)\s*:\s*([0-9\.]+)', output)
            modulus_match = re.search(r'- 단면계수\(Z\)\s*:\s*([0-9\.]+)', output)
            elastic_match = re.search(r'- 탄성계수\(E\)\s*:\s*([0-9\.]+)', output)
            yield_match = re.search(r'- 항복응력\(Fy\)\s*:\s*([0-9\.]+)', output)
            load_match = re.search(r'=> 최대 허용 사용하중\s*:\s*([0-9\.]+)', output)

            # 성공적으로 하중 결과값을 찾았을 경우
            if load_match:
                # 추출된 값 변수 할당 (실패 시 "-" 처리)
                member_area = area_match.group(1) if area_match else "-"
                member_inertia = inertia_match.group(1) if inertia_match else "-"
                member_radius = radius_match.group(1) if radius_match else "-"
                member_modulus = modulus_match.group(1) if modulus_match else "-"
                member_elastic = elastic_match.group(1) if elastic_match else "-"
                member_yield = yield_match.group(1) if yield_match else "-"
                safe_load = load_match.group(1)

                return render_template(
                    'columnBuckling.html',
                    title='Hi-TESS Column Buckling',
                    calculated=True,
                    member_name=member_select,
                    member_area=member_area,
                    member_inertia=member_inertia,
                    member_radius=member_radius,  # 새로 추가된 변수들
                    member_modulus=member_modulus,
                    member_elastic=member_elastic,
                    member_yield=member_yield,
                    member_length=length,
                    safe_load=safe_load
                )
            else:
                # C# 프로그램 내부 에러 등으로 결괏값이 없을 경우
                flash("계산 중 오류가 발생했습니다. 입력값을 확인하세요.")
                print(f"[C# Error Log] {result.stderr or output}")
                return render_template('columnBuckling.html', title='Hi-TESS Column Buckling', calculated=False)

        except Exception as e:
            flash(f"C# 해석 엔진 실행 실패: {str(e)}")
            return render_template('columnBuckling.html', title='Hi-TESS Column Buckling', calculated=False)

    else:
        # GET 요청 (최초 페이지 접속 시)
        user_permissions = session.get('permissions', {})
        programName = "기둥 좌굴 해석"  # 권한 DB에 등록된 명칭으로 수정

        # 권한 체크 (beam의 로직 차용)
        # if programName in user_permissions and user_permissions[programName] == True:
        return render_template('BlockColumnBuckling.html', title='Hi-TESS Column Buckling', calculated=False)
        # else:
        #     flash("프로그램 권한 신청이 필요합니다.")
        #     return redirect(url_for('index')) # root 페이지로 리다이렉트
