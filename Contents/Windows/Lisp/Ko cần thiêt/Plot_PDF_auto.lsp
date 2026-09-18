(defun pt2d (pt)
  (vlax-make-variant
    (vlax-safearray-fill
      (vlax-make-safearray vlax-vbDouble '(0 . 1))
      (list (car pt) (cadr pt))
    )
  )
)

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

(defun folderHasPDF (folder / fso files found)
  (vl-load-com)
  (setq fso (vlax-create-object "Scripting.FileSystemObject"))
  (setq files (vlax-get (vlax-invoke-method fso 'GetFolder folder) 'Files))
  (setq found nil)
  (vlax-for f files
    (if (wcmatch (strcase (vlax-get f 'Name)) "*.PDF")
      (setq found T)
    )
  )
  (vlax-release-object fso)
  found
)

(defun hasObjectsInWindow (ll ur / ss)
  (setq ss (ssget "_W" ll ur))
  (if ss T nil)
)

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

(defun JoinPath (folder filename /)
  (if (not folder) (setq folder ""))
  (if (not filename) (setq filename ""))
  (cond
    ((wcmatch folder "*\\") (strcat folder filename))
    ((wcmatch folder "*/")  (strcat folder filename))
    (T (strcat folder "\\" filename))
  )
)

(defun drawPreviewRect (ll ur / p1 p2 p3 p4)
  (setq p1 ll)
  (setq p2 (list (car ur) (cadr ll)))
  (setq p3 ur)
  (setq p4 (list (car ll) (cadr ur)))
  (command "_.layer" "_make" "PDF_PREVIEW" "")
  (command "_.pline" p1 p2 p3 p4 "C")
)

(defun c:PDFMODEL (/ acad doc layout plotConfig num name paperSize
                      outFolder p1 p2 p3 dx dy i ll ur fname fullpath plotObj doPreview)

  (vl-load-com)
  (setq acad (vlax-get-acad-object))
  (setq doc (vla-get-ActiveDocument acad))
  (setq layout (vla-get-ActiveLayout doc))
  (setq plotObj (vla-get-Plot doc))
  (setq plotConfig "AutoCAD PDF (High Quality Print).pc3")

  (setq num (getint "\nNhập số lượng bản vẽ cần in: "))
  (if (not num) (exit))

  (setq name (getstring t "\nNhập tên file PDF gốc (VD: BEAM_A): "))
  (if (= name "") (setq name "OUTPUT"))

  (setq paperSize (ChoosePaperSize layout))
  (vla-put-CanonicalMediaName layout paperSize)

  (setq outFolder (getfolder "Chọn thư mục lưu PDF"))
  (if (not outFolder) (exit))

  (if (folderHasPDF outFolder)
    (prompt "\n⚠️ Thư mục đã có file PDF. Có thể bị ghi đè.")
    (prompt "\n✅ Thư mục trống hoặc chưa có file PDF.")
  )

  ;; cấu hình Plot
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

  ;; vòng lặp in từng vùng
  (setq i 0)
  (while (< i num)
    (princ (strcat "\n→ Trang " (itoa (1+ i)) ": Chọn vùng in"))
    (setq p1 (getpoint "\n1) Góc trái-dưới: "))
    (setq p2 (getpoint "\n2) Góc phải-dưới: "))
    (setq p3 (getpoint "\n3) Góc trái-trên: "))

    ;; đặt UCS về điểm gốc
    (command "_.ucs" "_origin" p1)

    (setq dx (abs (- (car p2) (car p1))))
    (setq dy (abs (- (cadr p3) (cadr p1))))
    (prompt (strcat "\nKích thước vùng: dx=" (rtos dx 2 2) ", dy=" (rtos dy 2 2)))

    (setq ll (list (car p1) (cadr p1)))
    (setq ur (list (car p2) (cadr p3)))

    ;; vẽ khung preview
    (drawPreviewRect ll ur)

    (if (hasObjectsInWindow ll ur)
      (progn
        (vla-SetWindowToPlot layout (pt2d ll) (pt2d ur))
        (vla-RefreshPlotDeviceInfo layout)

        (initget "Yes No")
        (setq doPreview (getkword "\nXem trước vùng in? [Yes/No] <No>: "))
        (if (= doPreview "Yes")
          (command "_.plot" "_n" "" "" "_preview")
        )

        (setq fname (strcat name "_" (itoa (1+ i)) ".pdf"))
        (setq fullpath (JoinPath outFolder fname))
        (princ (strcat "\n→ Đang in: " fullpath))
        (vla-PlotToFile plotObj fullpath plotConfig)
      )
      (princ "\n⚠️ Vùng in không có đối tượng. Bỏ qua.")
    )

    (setq i (1+ i))
  )

  (princ "\nHoàn thành in PDF.")
  (princ)
)
