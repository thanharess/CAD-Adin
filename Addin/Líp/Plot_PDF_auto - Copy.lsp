;;; PDFMODEL_ACTIVEX.LSP
;;; In hàng loạt vùng trong Model sang PDF (dịch theo trục X)
;;; Dùng ActiveX nên chạy ổn định trên AutoCAD 2014–2025
;;; Lệnh: PDFMODEL

;;; ---- Hàm ép tọa độ về 2D (X,Y) ----
(defun pt2d (pt)
  (vlax-make-variant
    (vlax-safearray-fill
      (vlax-make-safearray vlax-vbDouble '(0 . 1))
      (list (car pt) (cadr pt))
    )
  )
)

;;; ---- Hàm chọn khổ giấy bằng số thứ tự ----
(defun ChoosePaperSize (layout / mediaNames i idx)
  (setq mediaNames
    (vlax-safearray->list
      (vlax-variant-value (vla-GetCanonicalMediaNames layout))
    )
  )
  (princ "\nDanh sách khổ giấy có sẵn:")
  (setq i 0)
  (foreach m mediaNames
    (princ (strcat "\n[" (itoa i) "] " m))
    (setq i (1+ i))
  )
  (setq idx (getint "\nNhập số thứ tự khổ giấy: "))
  (if (and idx (>= idx 0) (< idx (length mediaNames)))
    (nth idx mediaNames)
    (progn
      (princ "\nSố không hợp lệ, mặc định chọn mục 0.")
      (car mediaNames)
    )
  )
)

(defun c:PDFMODEL (/ acad doc layout plotConfig num name paperSize
                      outFolder p1 p2 p3 dx dy i offset ll ur fname fullpath plotObj)

  (vl-load-com)
  (setq acad (vlax-get-acad-object))
  (setq doc (vla-get-ActiveDocument acad))
  (setq layout (vla-get-ActiveLayout doc))

  ;; --- cấu hình máy in PDF ---
  (setq plotConfig "AutoCAD PDF (High Quality Print).pc3")

  ;; --- nhập số bản vẽ ---
  (setq num (getint "\nNhập số lượng bản vẽ cần in: "))
  (if (not num) (exit))

  ;; --- nhập tên file PDF gốc ---
  (setq name (getstring t "\nNhập tên file PDF gốc (VD: BEAM_A): "))
  (if (= name "") (setq name "OUTPUT"))

  ;; --- chọn khổ giấy ---
  (setq paperSize (ChoosePaperSize layout))
  (vla-put-CanonicalMediaName layout paperSize)

  ;; --- chọn thư mục lưu ---
  (setq outFolder (getfolder "Chọn thư mục lưu PDF"))
  (if (not outFolder) (exit))

  ;; --- chọn 3 điểm ---
  (princ "\nChọn 3 điểm để xác định kích thước vùng in:")
  (setq p1 (getpoint "\n1) Góc trái-dưới: "))
  (setq p2 (getpoint "\n2) Góc phải-dưới: "))
  (setq p3 (getpoint "\n3) Góc trái-trên: "))

  (setq dx (abs (- (car p2) (car p1))))
  (setq dy (abs (- (cadr p3) (cadr p1))))
  (prompt (strcat "\nKích thước vùng: dx=" (rtos dx 2 2) ", dy=" (rtos dy 2 2)))

  ;; --- cấu hình Plot ---
  (vla-put-ConfigName layout plotConfig)
  (vla-put-CanonicalMediaName layout paperSize)
  (vla-put-PlotRotation layout ac0degrees)
  (vla-put-PlotType layout acWindow)
  (vla-put-UseStandardScale layout :vlax-true)
  (vla-put-StandardScale layout acScaleToFit)
  (vla-put-CenterPlot layout :vlax-true)
  (vla-put-PlotWithPlotStyles layout :vlax-true)
  (vla-put-StyleSheet layout "monochrome.ctb")
  (vla-put-PlotWithLineweights layout :vlax-true)

  ;; --- vòng lặp in ---
  (setq i 0)
  (setq plotObj (vla-get-Plot doc))
  (while (< i num)
    (setq offset (* i dx))
    (setq ll (list (+ (car p1) offset) (cadr p1)))
    (setq ur (list (+ (car p2) offset) (cadr p3)))

    (vla-SetWindowToPlot layout (pt2d ll) (pt2d ur))

    (setq fname (strcat name "_" (itoa (1+ i)) ".pdf"))
    (setq fullpath (JoinPath outFolder fname))
    (princ (strcat "\n→ Đang in: " fullpath))

    (vla-RefreshPlotDeviceInfo layout)
    (vla-PlotToFile plotObj fullpath plotConfig)

    (setq i (1+ i))
  )

  (princ "\nHoàn thành in PDF (ActiveX).")
  (princ)
)

;;; ---- Hàm chọn thư mục ----
(defun getfolder (msg / sh folder)
  (vl-load-com)
  (setq sh (vla-getInterfaceObject (vlax-get-acad-object) "Shell.Application"))
  (setq folder (vlax-invoke-method sh 'BrowseForFolder 0 msg 0))
  (if folder
    (setq folder
      (vlax-get-property (vlax-get-property folder 'Self) 'Path)
    )
  )
  (vlax-release-object sh)
  folder
)

;;; ---- Hàm nối đường dẫn ----
(defun JoinPath (folder filename /)
  (if (not folder) (setq folder ""))
  (if (not filename) (setq filename ""))
  (cond
    ((wcmatch folder "*\\") (strcat folder filename))
    ((wcmatch folder "*/")  (strcat folder filename))
    (T (strcat folder "\\" filename))
  )
)
